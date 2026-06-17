using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.Core.Utilities;
using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

public sealed record TextureRoundTripRequest(string Filename, TextureBase Header, bool SkipImport = false) : IOperationRequest;

public sealed record TextureRoundTripResult(
    string Filename,
    bool PushImplemented,
    List<PropertyMismatch> Mismatches);

internal sealed class TextureRoundTripOperation(
    IResourceSerializer resourceSerializer,
    IMessageSink messageSink)
    : IOperation<TextureRoundTripRequest, TextureRoundTripResult>
{
    public Task<OperationResult<TextureRoundTripResult>> ExecuteAsync(
        TextureRoundTripRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool pushImplemented = true;
        try
        {
            request.Header.PushAll();
        }
        catch (NotImplementedException)
        {
            pushImplemented = false;
            messageSink.Verbose(
                $"A push isn't implemented for {request.Header.GetType().Name}!",
                MessageCategory.Autotest,
                nameof(TextureRoundTripOperation));
        }

        try
        {
            using (FileStream fs = new(request.Filename, FileMode.Create))
            {
                using (ResourceBinaryWriter writer = new(fs, request.Header.ResourceEndian))
                {
                    messageSink.Info(
                        $"AUTOTEST - Writing autotest {request.Filename} to working directory...",
                        MessageCategory.Autotest,
                        nameof(TextureRoundTripOperation));
                    request.Header.WriteToStream(writer);
                    writer.Close();
                }
            }

            if (request.SkipImport)
            {
                return Task.FromResult(OperationResultFactory.Success(new TextureRoundTripResult(request.Filename, pushImplemented, [])));
            }

            TextureBase newHeader;
            using (FileStream fs = File.OpenRead(request.Filename))
            {
                newHeader = (TextureBase)resourceSerializer.Deserialize(
                    fs,
                    ResourceType.Texture,
                    request.Header.ResourcePlatform,
                    new ResourceSerializationOptions
                    {
                        FileName = request.Filename,
                        x64 = request.Header.ResourceArch == Arch.x64
                    });
            }

            List<PropertyMismatch> mismatches = ResourcePropertyComparer.Compare(request.Header, newHeader);

            progress?.Report(new OperationProgress("texture-roundtrip", 1.0, request.Filename));
            return Task.FromResult(OperationResultFactory.Success(new TextureRoundTripResult(request.Filename, pushImplemented, mismatches)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResultFactory.Failure<TextureRoundTripResult>(
                "texture_roundtrip_failed",
                ex.Message,
                nameof(TextureRoundTripOperation)));
        }
    }
}
