namespace MarketMagic

open System
open System.IO
open Tomlyn
open Tomlyn.Model
open System.Runtime.CompilerServices
open System.Collections.Generic

[<Extension>]
type TomlTableExtensions() =

    [<Extension>]
    static member private TryGetItem<'t>(self: TomlTable, key: string) : 't option =
        match self.TryGetValue(key) with
        | true, value ->
            match box value with
            | :? 't as typed -> Some typed
            | objValue ->
                try
                    Convert.ChangeType(objValue, typeof<'t>) :?> 't |> Some
                with _ ->
                    None
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

    [<Extension>]
    static member TrySet<'t>(self: TomlTable, path: string, value: 't) : bool =
        let rec loop (tbl: TomlTable) (ps: string list) =
            match ps with
            | [] -> false
            | [last] ->
                tbl.[last] <- value
                true
            | hd :: tl ->
                match tbl.TryGetItem<TomlTable>(hd) with
                | Some subtbl -> loop subtbl tl
                | None ->
                    let newTbl = TomlTable()
                    tbl.[hd] <- newTbl
                    loop newTbl tl
        path.Split '.' |> List.ofArray |> loop self

    [<Extension>]
    static member Set<'t>(self: TomlTable, path: string, value: 't) : unit =
        if not <| self.TrySet(path, value) then
            raise (KeyNotFoundException($"Could not set value for path '{path}' in TomlTable."))