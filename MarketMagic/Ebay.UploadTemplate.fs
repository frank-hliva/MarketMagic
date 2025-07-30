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
        cells : string[,]
    }

type CommandResponse(success : bool) = 
    member self.Success = success

type CommandMessageResponse(success : bool, message : string, error : string) = 
    inherit CommandResponse(success)
    member self.Message = message
    member self.Error = error

type CommandDataResponse<'t>(success : bool, data : 't) = 
    inherit CommandResponse(success)
    member self.Data = data

type CommandSaveResponse(success : bool) = 
    inherit CommandResponse(success)

let sendCommand<'t> serverAddress (json : string) =
    use client = new RequestSocket()
    client.Connect(serverAddress)
    client.SendFrame(json)
    let response = client.ReceiveFrameString()
    printfn "Response: %s" response
    JSON.parse<'t>(response)

type UploadTemplateConfig(appConfig : AppConfig) =

    member self.AppConfig = appConfig

    member private self.Address =
        appConfig.GetOr("Engine.Server.Address", "tcp://localhost")

    member self.Port =
        appConfig.GetOr("Engine.Server.Port", 7333)

    member self.FullAddress = self.Address |> Url.withPort self.Port

type UploadTemplate(uploadTemplateConfig : UploadTemplateConfig) =

    let serverAddress = uploadTemplateConfig.FullAddress

    member self.Load (path : string) =
        sprintf """{"command": "loadUploadTemplate", "path": "%s"}""" path
        |> sendCommand<CommandMessageResponse> serverAddress

    member self.Fetch () =
        """{"command": "fetchUploadTemplate"}"""
        |> sendCommand<CommandDataResponse<UploadDataTable>> serverAddress

    member self.AddExportedData (path : string) =
        sprintf """{"command": "addExportedData", "path":"%s"}""" path
        |> sendCommand<CommandMessageResponse> serverAddress

    member self.Save (path : string) =
        sprintf """{"command": "saveUploadTemplate", "path": "%s"}""" path
        |> sendCommand<CommandSaveResponse> serverAddress