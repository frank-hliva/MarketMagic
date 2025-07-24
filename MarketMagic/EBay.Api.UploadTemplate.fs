module MarketMagic.EBay.Api

open System
open NetMQ
open NetMQ.Sockets
open FSharp.Data

let [<Literal>] serverAddress = "tcp://localhost:5555"

let private sendCommand (json:string) =
    use client = new RequestSocket()
    client.Connect(serverAddress)
    client.SendFrame(json)
    let response = client.ReceiveFrameString()
    printfn "Response: %s" response
    response

module UploadTemplate =

    let fetch () =
        """{"command":"fetchUploadTemplate"}""" |> sendCommand

    let load (path:string) =
        sprintf """{"command":"loadUploadTemplate","path":"%s"}""" path
        |> sendCommand

    let addExportedData (path:string) =
        sprintf """{"command":"addExportedData","path":"%s"}""" path
        |> sendCommand 

    let save (path:string) =
        sprintf """{"command":"saveUploadTemplate","path":"%s"}""" path
        |> sendCommand