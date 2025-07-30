namespace MarketMagic

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection

[<Extension>]
type ServiceCollectionExtensions() =
    [<Extension>]
    static member RegisterCommonServices(self : IServiceCollection) =
        self
            .AddSingleton<IAppConfigProvider, AppConfigProvider>()
            .AddSingleton<AppConfig>(fun serviceProvider ->
                serviceProvider.GetRequiredService<IAppConfigProvider>().Config
            )
            .AddSingleton<EngineConfig>()
            .AddSingleton<Engine>()
            .AddSingleton<Ebay.UploadTemplateConfig>()
            .AddSingleton<Ebay.UploadTemplate>()
            .AddSingleton<TableViewModel>()
            .AddSingleton<MainWindow>()