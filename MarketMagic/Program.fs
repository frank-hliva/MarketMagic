module MarketMagic.Program

open System
open System.Diagnostics
open System.Threading
open Avalonia
open Microsoft.Extensions.DependencyInjection

[<CompiledName "BuildAvaloniaApp">] 
let buildAvaloniaApp (serviceProvider: IServiceProvider) =
    AppBuilder
        .Configure(fun () -> App(serviceProvider))
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace(areas = Array.empty)

[<EntryPoint; STAThread>]
let main argv =
    let serviceProvider =
        ServiceCollection()
            .RegisterCommonServices()
            .BuildServiceProvider()    
    if serviceProvider.GetRequiredService<Engine>().Start().WaitForReady 5000 then
        printfn "Backend is ready. Starting frontend..."
        buildAvaloniaApp(serviceProvider)
            .StartWithClassicDesktopLifetime(argv)
    else
        printfn "Backend did not start in time."
        1