using Microsoft.Extensions.DependencyInjection;
using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
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
}
