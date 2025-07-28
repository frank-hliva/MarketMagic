namespace MarketMagic

open System.IO
open Tomlyn

type IAppConfigProvider =
    abstract member Config : obj

type AppConfigProvider(configPath: string) =
    let config =
        configPath
        |> File.ReadAllText
        |> Toml.ToModel

    interface IAppConfigProvider with
        member self.Config = config

    member self.Config = config