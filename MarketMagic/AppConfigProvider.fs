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

[<Extension>]
type TomlTableExtensions() =

    [<Extension>]
    static member private TryGetItem<'t>(self: TomlTable, key: string) : 't option =
        match self.TryGetValue(key) with
        | true, value ->
            match box value with
            | :? 't as typed -> Some typed
            | _ -> None
        | _ -> None

    [<Extension>]
    static member TryGet<'t> (self: TomlTable, path: string) : 't option =
        let rec loop (tbl: TomlTable) (ps: string list) =
            match ps with
            | [] -> None
            | [last] -> tbl.TryGetItem<'t>(last)
            | hd :: tl ->
                match tbl.TryGetItem<TomlTable>(hd) with
                | Some subtbl -> loop subtbl tl
                | None -> None
        path.Split '.' |> List.ofArray |> loop self

    [<Extension>]
    static member Get<'t>(self: TomlTable, key: string) : 't =
        match self.TryGet<'t>(key) with
        | Some value -> value
        | None -> raise (KeyNotFoundException($"Key '{key}' not found or cannot be cast to type {typeof<'t>.FullName}."))

    [<Extension>]
    static member GetOr<'t>(self: TomlTable, key: string, defaultValue: 't) : 't =
        match self.TryGet<'t>(key) with
        | Some value -> value
        | None -> defaultValue