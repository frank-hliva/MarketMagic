namespace MarketMagic

open System
open System.Diagnostics
open NetMQ
open NetMQ.Sockets
open System.Threading

type Engine(engineConfig : EngineConfig) as self =

    do
        if engineConfig.Start then self.Start() |> ignore

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

and EngineConfig(appConfig : AppConfig) =

    member self.AppConfig = appConfig

    member self.Interpreter =
        appConfig.GetOr("Engine.Interpreter", "julia")

    member self.Path =
        appConfig.GetOr("Engine.Path", "../../../../Engine/src/MarketMagic.jl")

    member self.Start =
        appConfig.GetOr("Engine.Start", true)

    member private self.Address =
        appConfig.GetOr("Engine.Server.Address", "tcp://localhost")

    member self.Port =
        appConfig.GetOr("Engine.Server.Port", 7333)

    member self.FullAddress = self.Address |> Url.withPort self.Port

    member self.ConnectionTimeout =
        appConfig.GetOr("Engine.Server.ConnectionTimeout", 20000.0)

    member self.IterationTimeout =
        appConfig.GetOr("Engine.Server.IterationTimeout", 500.0)