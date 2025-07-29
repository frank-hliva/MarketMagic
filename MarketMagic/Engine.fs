namespace MarketMagic

open System
open System.Diagnostics
open NetMQ
open NetMQ.Sockets
open System.Threading
open Tomlyn.Model

type Engine(appConfig : AppConfig) =

    member self.Start() =
        ProcessStartInfo(
            FileName = "julia",
            Arguments = "../../../../Engine/src/MarketMagic.jl",
            UseShellExecute = false,
            CreateNoWindow = true
        )
        |> Process.Start
        |> ignore
        self

    member self.WaitForReady(timeoutMs : int) =
        let startTime = DateTime.Now
        let rec loop () =
            if (DateTime.Now - startTime).TotalMilliseconds >= float timeoutMs then
                false
            else
                try
                    use client = new RequestSocket()
                    client.Connect("tcp://localhost:5555")
                    let isSent = client.TrySendFrame(TimeSpan.FromMilliseconds(500.0), """{"command":"fetchUploadTemplate"}""")
                    let isReceived, _ = client.TryReceiveFrameString(TimeSpan.FromMilliseconds(500.0))
                    if isSent && isReceived then
                        true
                    else
                        Thread.Sleep(500)
                        loop ()
                with
                | _ ->
                    Thread.Sleep(500)
                    loop ()
        loop ()