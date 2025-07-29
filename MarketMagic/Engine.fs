namespace MarketMagic

open System
open System.Diagnostics
open NetMQ
open NetMQ.Sockets
open System.Threading

type Engine() =

    member self.Start() =
        ProcessStartInfo(
            FileName = "julia",
            Arguments = "../../../../Engine/src/MarketMagic.jl",
            UseShellExecute = false,
            CreateNoWindow = true
        )
        |> Process.Start
        |> ignore

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

    member self.Setup() =
        self.Start()
        match self.WaitForReady 5000 with
        | true -> 
            printfn "Backend is ready. Starting frontend..."
            0
        | _ ->
            printfn "Backend did not start in time."
            1