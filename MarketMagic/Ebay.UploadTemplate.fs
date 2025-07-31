module MarketMagic.Ebay

open System
open Newtonsoft.Json
open NetMQ
open NetMQ.Sockets
open FSharp.Data
open Lime

let inline sendRawCommand<'t> serverAddress (json : string) =
    use client = new RequestSocket()
    client.Connect(serverAddress)
    client.SendFrame(json)
    client.ReceiveFrameString()
    |> JSON.parse<'t>

let sendCommand<'t> serverAddress (requestObj : obj) =
    requestObj
    |> JSON.stringify
    |> sendRawCommand<'t> serverAddress

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

type UploadTemplateManager(uploadTemplateManagerConfig : UploadTemplateManagerConfig) =

    let serverAddress = uploadTemplateManagerConfig.FullAddress

    member private self.SendCommand<'t> (requestObj : obj) =
        sendCommand<'t> serverAddress requestObj

    member self.Load (path : string) =
        {| command = "loadUploadTemplate"; path = path |}
        |> self.SendCommand<CommandMessageResponse>

    member self.Fetch () =
        {| command = "fetchUploadTemplate" |}
        |> self.SendCommand<CommandDataResponse<UploadDataTable>>

    member self.LoadDocument (path : string) =
        {| command = "loadDocument"; path = path |}
        |> self.SendCommand<CommandMessageResponse>

    member self.Save (path : string, uploadDataTable : UploadDataTable) =
        {| command = "saveUploadTemplate"; path = path; uploadDataTable = uploadDataTable |}
        |> self.SendCommand<CommandSaveResponse>

and UploadTemplateManagerConfig(appConfig : AppConfig) =

    member self.AppConfig = appConfig

    member private self.Address =
        appConfig.GetOr("Engine.Server.Address", "tcp://localhost")

    member self.Port =
        appConfig.GetOr("Engine.Server.Port", 7333)

    member self.FullAddress = self.Address |> Url.withPort self.Port