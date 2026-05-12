using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.Messaging;
using Volatility.Operations.Autotest;
using Volatility.Operations.Resources;
using Volatility.Operations.StringTables;
using Volatility.Services;

namespace Volatility.Hosting;

public static class VolatilityServiceCollectionExtensions
{
    public static IServiceCollection AddVolatilityCore(this IServiceCollection services)
    {
        services.TryAddSingleton<MessageBus>();
        services.TryAddSingleton<IMessageBus>(serviceProvider => serviceProvider.GetRequiredService<MessageBus>());
        services.TryAddSingleton<IMessageSink>(serviceProvider => serviceProvider.GetRequiredService<IMessageBus>());

        services.TryAddSingleton<IPathProvider, EnvironmentPathProvider>();
        services.TryAddSingleton<IProcessRunner, DefaultProcessRunner>();
        services.TryAddSingleton<IShaderCompiler, DefaultShaderCompiler>();
        services.TryAddSingleton<IResourceFactory, DefaultResourceFactory>();
        services.TryAddSingleton<IResourceDBLookup, FileResourceDBLookup>();
        services.TryAddSingleton<IStringTableStore, FileStringTableStore>();
        services.TryAddSingleton<ITextureBitmapStore, FileTextureBitmapStore>();
        services.TryAddSingleton<IShaderSourceStore, FileShaderSourceStore>();
        services.TryAddSingleton<ISplicerSampleStore, FileSplicerSampleStore>();

        services.AddTransient<IOperation<CreateResourceRequest, CreateResourceResult>, CreateResourceOperation>();
        services.AddTransient<IOperation<LoadResourceRequest, LoadResourceResult>, LoadResourceOperation>();
        services.AddTransient<IOperation<SaveResourceRequest, SaveResourceResult>, SaveResourceOperation>();
        services.AddTransient<IOperation<CreateShaderProgramBufferRequest, CreateShaderProgramBufferResult>, CreateShaderProgramBufferOperation>();
        services.AddTransient<IOperation<LoadResourceDictionaryRequest, LoadResourceDictionaryResult>, LoadResourceDictionaryOperation>();
        services.AddTransient<IOperation<MergeStringTableEntriesRequest, MergeStringTableEntriesResult>, MergeStringTableEntriesOperation>();

        // Temporary registrations for operations that are not on the OperationResult contract yet
        services.AddTransient<ImportResourceOperation>();
        services.AddTransient<ImportStringTableOperation>();
        services.AddTransient<ExportResourceOperation>();
        services.AddTransient<TextureToDDSOperation>();
        services.AddTransient<PortTextureOperation>();
        services.AddTransient<GameAutotestOperation>();

        return services;
    }
}
