namespace MarketMagic

open Avalonia.Data.Converters
open System
open System.Globalization

type StringSplitConverter() =
    static member val Instance = StringSplitConverter()
    
    interface IValueConverter with
        member this.Convert(value, targetType, parameter, culture) =
            match value, parameter with
            | (:? string as str), (:? string as indexStr) ->
                let parts = str.Split(',')
                match Int32.TryParse(indexStr) with
                | true, index when index < parts.Length -> box parts.[index]
                | _ -> box ""
            | _ -> box ""
        
        member this.ConvertBack(value, targetType, parameter, culture) =
            raise (NotSupportedException())