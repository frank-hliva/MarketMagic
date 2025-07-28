namespace MarketMagic

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Microsoft.Extensions.DependencyInjection
open System
open System.IO
open MarketMagic


type App() =
    inherit Application()

    let serviceProvider =
        ServiceCollection()
            .RegisterCommonServices()
            .BuildServiceProvider()

    member self.ServiceProvider = serviceProvider

    override self.Initialize() =
        AvaloniaXamlLoader.Load(self)

    override self.OnFrameworkInitializationCompleted() =
        match self.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.MainWindow <- serviceProvider.GetRequiredService<MainWindow>()
        | _ -> ()
        base.OnFrameworkInitializationCompleted()