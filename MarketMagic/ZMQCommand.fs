namespace MarketMagic

open System
open FSharp.Data
open Newtonsoft.Json
open NetMQ
open NetMQ.Sockets
open Lime
open MarketMagic

type DataTable = 
    {
        id : int64
        columns : string list
        enums : Map<string, string list>
        cells : string[,]
    }

module ZMQCommand =

    let inline sendRaw<'t> serverAddress (json : string) =
        use client = new RequestSocket()
        client.Connect(serverAddress)
        client.SendFrame(json)
        client.ReceiveFrameString()
        |> JSON.parse<'t>

    let send<'t> serverAddress (requestObj : obj) =
        requestObj
        |> JSON.stringify
        |> sendRaw<'t> serverAddress

type CommandResponse(success : bool) = 
    member self.Success = success

type CommandMessageResponse(
    success : bool,
    message : string,
    error : string,
    internalError : string
) = 
    inherit CommandResponse(success)
    member self.Message = message
    member self.Error = error
    member self.InternalError = internalError

type CommandDataResponse<'t>(success : bool, data : 't) = 
    inherit CommandResponse(success)
    member self.Data = data

type CommandSaveResponse(success : bool) = 
    inherit CommandResponse(success)

type ZMQCommandManager(zmqServerConfig : ZMQServerConfig) =
    let serverAddress = zmqServerConfig.FullAddress

    member self.SendCommand<'t> (requestObj : obj) =
        ZMQCommand.send<'t> serverAddress requestObj

and ZMQServerConfig(appConfig : AppConfig) =

    member self.AppConfig = appConfig

    member private self.Address =
        appConfig.GetOr("Engine.Server.Address", "tcp://localhost")

    member self.Port =
        appConfig.GetOr("Engine.Server.Port", 7333)

    member self.FullAddress = self.Address |> Url.withPort self.Port