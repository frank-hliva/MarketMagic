namespace MarketMagic

open System
open System.IO
open Tomlyn
open Tomlyn.Model
open System.Runtime.CompilerServices
open System.Collections.Generic

type IAppConfigProvider =
    abstract member Config : TomlTable

type AppConfigProvider() =
    let config =
        MarketMagicApp.appConfig
        |> File.ReadAllText
        |> Toml.ToModel

    interface IAppConfigProvider with
        member self.Config = config

    member self.Config = config