namespace MarketMagic

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Microsoft.Extensions.DependencyInjection
open System
open System.IO
open MarketMagic

type App(serviceProvider: IServiceProvider) =
    inherit Application()

    member self.ServiceProvider = serviceProvider

    override self.Initialize() =
        AvaloniaXamlLoader.Load(self)

    member self.HandleExit(event : ControlledApplicationLifetimeExitEventArgs) =
        serviceProvider
            .GetRequiredService<AppConfig>()
            .Save(MarketMagicApp.appConfig)

    override self.OnFrameworkInitializationCompleted() =
        match self.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.MainWindow <- serviceProvider.GetRequiredService<MainWindow>()
            desktop.Exit.Add(self.HandleExit)
        | _ -> ()
        base.OnFrameworkInitializationCompleted()