namespace MarketMagic

open System
open System.Diagnostics
open NetMQ
open NetMQ.Sockets
open System.Threading
open Tomlyn.Model

type EngineConfig(appConfig : AppConfig) =
    let addressWithPort (addr : string) (port : int) =            
        $"""{if addr.EndsWith("/") then addr[..addr.Length - 2] else addr}:{port}"""

    member self.AppConfig = appConfig

    member self.Interpreter =
        appConfig.GetOr("Engine.Interpreter", "julia")

    member self.Path =
        appConfig.GetOr("Engine.Path", "../../../../Engine/src/MarketMagic.jl")

    member private self.Address =
        appConfig.GetOr("Engine.Server.Address", "tcp://localhost")

    member self.Port =
        appConfig.GetOr("Engine.Server.Port", 7333)

    member self.FullAddress =
        addressWithPort self.Address self.Port

    member self.ConnectionTimeout =
        appConfig.GetOr("Engine.Server.ConnectionTimeout", 5000.0)

    member self.IterationTimeout =
        appConfig.GetOr("Engine.Server.IterationTimeout", 500.0)

type Engine(engineConfig : EngineConfig) =

    member self.Start() =
        ProcessStartInfo(
            FileName = engineConfig.Interpreter,
            Arguments = engineConfig.Path,
            UseShellExecute = false,
            CreateNoWindow = true
        )
        |> Process.Start
        |> ignore
        self

    member self.WaitForReady() =
        let startTime = DateTime.Now
        let timeout = engineConfig.IterationTimeout
        let rec loop () =
            if (DateTime.Now - startTime).TotalMilliseconds >= engineConfig.ConnectionTimeout then
                false
            else
                try
                    use client = new RequestSocket()
                    client.Connect(engineConfig.FullAddress)
                    let isSent = client.TrySendFrame(TimeSpan.FromMilliseconds(timeout), """{"command": "fetchUploadTemplate"}""")
                    let isReceived, _ = client.TryReceiveFrameString(TimeSpan.FromMilliseconds(timeout))
                    if isSent && isReceived then
                        true
                    else
                        Thread.Sleep(int timeout)
                        loop ()
                with
                | _ ->
                    Thread.Sleep(int timeout)
                    loop ()
        loop ()