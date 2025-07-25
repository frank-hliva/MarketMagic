module MarketMagic.Backend

open System
open System.Diagnostics
open NetMQ
open NetMQ.Sockets
open System.Threading
open Avalonia

let start () =
    ProcessStartInfo(
        FileName = "julia",
        Arguments = "../../../../Engine/src/MarketMagic.jl",
        UseShellExecute = false,
        CreateNoWindow = true
    ) |> Process.Start |> ignore

let rec waitForReady (timeoutMs: int) =
    let startTime = DateTime.Now
    let mutable connected = false
    while not connected && (DateTime.Now - startTime).TotalMilliseconds < float timeoutMs do
        try
            use client = new RequestSocket()
            client.Connect("tcp://localhost:5555")
            client.SendFrame("{\"command\":\"fetchUploadTemplate\"}")
            let _ = client.ReceiveFrameString()
            connected <- true
        with
        | _ -> Thread.Sleep(500)
    connected