module MarketMagic.Program

open System
open System.Diagnostics
open NetMQ
open NetMQ.Sockets
open System.Threading
open Avalonia

[<CompiledName "BuildAvaloniaApp">] 
let buildAvaloniaApp () = 
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace(areas = Array.empty)

[<EntryPoint; STAThread>]
let main argv =
    Backend.start()
    match Backend.waitForReady 5000 with
    | true -> 
        printfn "Backend is ready. Starting frontend..."
        buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
    | _ ->
        printfn "Backend did not start in time."
        1