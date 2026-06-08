using Microsoft.Extensions.DependencyInjection;
using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.Hosting;
using Volatility.Operations.Resources;
using Volatility.Resources;
using Xunit;

namespace Volatility.Core.Tests;

public class OperationIntegrationTests
{
    private class CaptureMessageSink : IMessageSink
    {
        public List<VolatilityMessage> Messages { get; } = [];

        public void Publish(in VolatilityMessage message)
        {
            Messages.Add(message);
        }
    }

    [Fact]
    public async Task TestTextureRoundTripOperationSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVolatilityCore();
        
        var captureSink = new CaptureMessageSink();
        services.AddSingleton<IMessageSink>(captureSink);
        
        var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMessageBus>().Subscribe(captureSink);

        var roundTripOp = sp.GetRequiredService<IOperation<TextureRoundTripRequest, TextureRoundTripResult>>();

        // Create a directory structure that satisfies:
        // DirectoryInfo(ImportedFileName).Parent.Parent.Name ends with "_GR"
        string tempDir = Path.Combine(Path.GetTempPath(), "VolatilityTests_" + Guid.NewGuid().ToString("N") + "_GR", "textures");
        Directory.CreateDirectory(tempDir);
        string tempFile = Path.Combine(tempDir, "test_texture.dat");

        var textureHeader = new TexturePC
        {
            AssetName = "test_texture",
            ResourceID = ResourceID.HashFromString("test_texture"),
            Format = D3DFORMAT.D3DFMT_DXT1,
            Width = 256,
            Height = 256,
            MipmapLevels = 1,
            UsageFlags = TextureBaseUsageFlags.GRTexture,
            ImportedFileName = tempFile
        };

        try
        {
            // Act
            var result = await roundTripOp.ExecuteAsync(
                new TextureRoundTripRequest(tempFile, textureHeader, SkipImport: false),
                progress: null,
                cancellationToken: CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.PushImplemented);
            Assert.Empty(result.Value.Mismatches);
            Assert.NotEmpty(captureSink.Messages);
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(tempDir)))
            {
                Directory.Delete(Path.GetDirectoryName(tempDir)!, true);
            }
        }
    }

    [Fact]
    public async Task TestTextureToDDSOperationSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVolatilityCore();

        var captureSink = new CaptureMessageSink();
        services.AddSingleton<IMessageSink>(captureSink);

        var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMessageBus>().Subscribe(captureSink);

        var serializer = sp.GetRequiredService<IResourceSerializer>();
        var toDdsOp = sp.GetRequiredService<IOperation<TextureToDDSRequest, TextureToDDSResult>>();

        string tempDir = Path.Combine(Path.GetTempPath(), "VolatilityTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string tempFile = Path.Combine(tempDir, "test_texture.dat");
        string tempBitmap = Path.Combine(tempDir, "test_texture_texture.dat");

        var textureHeader = new TexturePC
        {
            AssetName = "test_texture",
            ResourceID = ResourceID.HashFromString("test_texture"),
            Format = D3DFORMAT.D3DFMT_DXT1,
            Width = 16,
            Height = 16,
            MipmapLevels = 1,
            UsageFlags = TextureBaseUsageFlags.GRTexture,
            ImportedFileName = tempFile
        };

        // Write header using serializer
        using (FileStream fs = File.Create(tempFile))
        {
            serializer.Serialize(textureHeader, fs, new ResourceSerializationOptions { x64 = false });
        }

        // Write dummy bitmap data (8 bytes for DXT1 16x16)
        byte[] dummyBitmap = new byte[8];
        File.WriteAllBytes(tempBitmap, dummyBitmap);

        try
        {
            // Act
            var result = await toDdsOp.ExecuteAsync(
                new TextureToDDSRequest([tempFile], Platform.TUB, IsX64: false, OutputPath: tempDir, Overwrite: true, Verbose: true),
                progress: null,
                cancellationToken: CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value.OutputPaths);

            string expectedDds = Path.Combine(tempDir, "test_texture.dds");
            Assert.True(File.Exists(expectedDds));

            byte[] ddsBytes = File.ReadAllBytes(expectedDds);
            Assert.True(ddsBytes.Length > 4);
            Assert.Equal(0x44, ddsBytes[0]); // 'D'
            Assert.Equal(0x44, ddsBytes[1]); // 'D'
            Assert.Equal(0x53, ddsBytes[2]); // 'S'
            Assert.Equal(0x20, ddsBytes[3]); // ' '
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task TestPortTextureOperationSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVolatilityCore();

        var captureSink = new CaptureMessageSink();
        services.AddSingleton<IMessageSink>(captureSink);

        var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMessageBus>().Subscribe(captureSink);

        var serializer = sp.GetRequiredService<IResourceSerializer>();
        var portOp = sp.GetRequiredService<IOperation<PortTextureRequest, PortTextureResult>>();

        string tempDir = Path.Combine(Path.GetTempPath(), "VolatilityTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string tempFile = Path.Combine(tempDir, "test_port.dat");
        string tempBitmap = Path.Combine(tempDir, "test_port_texture.dat");

        var textureHeader = new TexturePC
        {
            AssetName = "test_port",
            ResourceID = ResourceID.HashFromString("test_port"),
            Format = D3DFORMAT.D3DFMT_DXT1,
            Width = 16,
            Height = 16,
            MipmapLevels = 1,
            UsageFlags = TextureBaseUsageFlags.GRTexture,
            ImportedFileName = tempFile
        };

        // Write header using serializer (TUB/PC source)
        using (FileStream fs = File.Create(tempFile))
        {
            serializer.Serialize(textureHeader, fs, new ResourceSerializationOptions { x64 = false });
        }

        // Write dummy bitmap data
        byte[] dummyBitmap = new byte[8];
        File.WriteAllBytes(tempBitmap, dummyBitmap);

        string destDir = Path.Combine(tempDir, "output");

        try
        {
            // Act
            var result = await portOp.ExecuteAsync(
                new PortTextureRequest(
                    SourceFiles: [tempFile],
                    SourceFormat: "TUB",
                    SourcePath: tempFile,
                    DestinationFormat: "BPR",
                    DestinationPath: destDir,
                    Verbose: true,
                    UseGTF: false),
                progress: null,
                cancellationToken: CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value.OutputPaths);

            string portedHeader = Path.Combine(destDir, "test_port.dat");
            string portedBitmap = Path.Combine(destDir, "test_port_texture.dat");

            Assert.True(File.Exists(portedHeader));
            Assert.True(File.Exists(portedBitmap));

            // Deserialize ported BPR header and check if it ported successfully
            using FileStream fs = File.OpenRead(portedHeader);
            var portedTexture = (TextureBPR)serializer.Deserialize(
                fs,
                ResourceType.Texture,
                Platform.BPR,
                new ResourceSerializationOptions { FileName = portedHeader });

            Assert.Equal(DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, portedTexture.Format);
            Assert.Equal((uint)16, portedTexture.Width);
            Assert.Equal((uint)16, portedTexture.Height);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
