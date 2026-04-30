using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<MessageBus>();
        services.AddSingleton<IMessageBus>(serviceProvider => serviceProvider.GetRequiredService<MessageBus>());
        services.AddSingleton<IMessageSink>(serviceProvider => serviceProvider.GetRequiredService<MessageBus>());

        services.AddSingleton<IPathProvider, EnvironmentPathProvider>();
        services.AddSingleton<IProcessRunner, DefaultProcessRunner>();
        services.AddSingleton<IShaderCompiler, DefaultShaderCompiler>();
        services.AddSingleton<IResourceDBLookup, FileResourceDBLookup>();
        services.AddSingleton<IStringTableStore, FileStringTableStore>();
        services.AddSingleton<ITextureBitmapStore, FileTextureBitmapStore>();
        services.AddSingleton<IShaderSourceStore, FileShaderSourceStore>();
        services.AddSingleton<ISplicerSampleStore, FileSplicerSampleStore>();

        services.AddTransient<CreateResourceOperation>(serviceProvider => 
            new(serviceProvider.GetRequiredService<IPathProvider>().GetDirectory(VolatilityPathLocation.Resources)));
        services.AddTransient<LoadResourceOperation>();
        services.AddTransient<SaveResourceOperation>();
        services.AddTransient<CreateShaderProgramBufferOperation>();
        services.AddTransient<ImportResourceOperation>();
        services.AddTransient<LoadResourceDictionaryOperation>();
        services.AddTransient<MergeStringTableEntriesOperation>();
        services.AddTransient<ImportStringTableOperation>(serviceProvider =>
            new(serviceProvider.GetRequiredService<IOperation<MergeStringTableEntriesRequest, MergeStringTableEntriesResult>>()));
        services.AddTransient<ExportResourceOperation>();
        services.AddTransient<TextureToDDSOperation>();
        services.AddTransient<PortTextureOperation>();
        services.AddTransient<GameAutotestOperation>();

        services.AddTransient<IOperation<CreateResourceRequest, CreateResourceResult>>(serviceProvider =>
            serviceProvider.GetRequiredService<CreateResourceOperation>());
        services.AddTransient<IOperation<LoadResourceRequest, LoadResourceResult>>(serviceProvider =>
            serviceProvider.GetRequiredService<LoadResourceOperation>());
        services.AddTransient<IOperation<SaveResourceRequest, SaveResourceResult>>(serviceProvider =>
            serviceProvider.GetRequiredService<SaveResourceOperation>());
        services.AddTransient<IOperation<CreateShaderProgramBufferRequest, CreateShaderProgramBufferResult>>(serviceProvider =>
            serviceProvider.GetRequiredService<CreateShaderProgramBufferOperation>());
        services.AddTransient<IOperation<LoadResourceDictionaryRequest, LoadResourceDictionaryResult>>(serviceProvider =>
            serviceProvider.GetRequiredService<LoadResourceDictionaryOperation>());
        services.AddTransient<IOperation<MergeStringTableEntriesRequest, MergeStringTableEntriesResult>>(serviceProvider =>
            serviceProvider.GetRequiredService<MergeStringTableEntriesOperation>());

        return services;
    }
}
