module MarketMagic.Ebay

open System
open NetMQ
open NetMQ.Sockets
open FSharp.Data
open Lime

type UploadDataTable = 
    {
        id : int64
        columns : string list
        enums : Map<string, string list>
        cells : string[][]
    }

let [<Literal>] serverAddress = "tcp://localhost:5555"

type CommandResponse(success : bool) = 
    member r.Success = success

type CommandMessageResponse(success : bool, message : string, error : string) = 
    inherit CommandResponse(success)
    member r.Message = message
    member r.Error = error

type CommandDataResponse<'t>(success : bool, data : 't) = 
    inherit CommandResponse(success)
    member r.Data = data

let private sendCommand<'t> (json : string) =
    use client = new RequestSocket()
    client.Connect(serverAddress)
    client.SendFrame(json)
    let response = client.ReceiveFrameString()
    printfn "Response: %s" response
    JSON.parse<'t>(response)

module UploadTemplate =

    let load (path : string) =
        sprintf "{\"command\":\"loadUploadTemplate\",\"path\":\"%s\"}" path
        |> sendCommand<CommandMessageResponse>

    let fetch () =
        """{"command":"fetchUploadTemplate"}"""
        |> sendCommand<CommandDataResponse<UploadDataTable>>

    let addExportedData (path : string) =
        sprintf """{"command":"addExportedData","path":"%s"}""" path
        |> sendCommand<CommandMessageResponse>

    let save (path : string) =
        sprintf """{"command":"saveUploadTemplate","path":"%s"}""" path
        |> sendCommand