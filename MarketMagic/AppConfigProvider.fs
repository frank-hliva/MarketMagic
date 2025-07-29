namespace rec MarketMagic

open System
open System.IO
open Tomlyn
open Tomlyn.Model
open System.Runtime.CompilerServices
open System.Collections.Generic

type AppConfig = TomlTable

type IAppConfigProvider =
    abstract member Config : AppConfig

type AppConfigProvider() =
    let config =
        MarketMagicApp.appConfig
        |> File.ReadAllText
        |> Toml.ToModel

    interface IAppConfigProvider with
        member self.Config = config

    member self.Config = config