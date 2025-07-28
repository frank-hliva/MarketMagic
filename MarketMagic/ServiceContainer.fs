namespace MarketMagic

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection

[<Extension>]
type ServiceCollectionExtensions() =
    [<Extension>]
    static member RegisterCommonServices(self : IServiceCollection) =
        self.AddSingleton<IAppConfigProvider, AppConfigProvider>() |> ignore
        self.AddSingleton<TableViewModel>() |> ignore
        self.AddSingleton<MainWindow>() |> ignore
        self