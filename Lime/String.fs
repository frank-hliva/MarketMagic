[<AutoOpen>]
module Lime.StringModule

open System
open System.Text
open System.Text.RegularExpressions
open System.Runtime.CompilerServices
open System.Globalization

[<Extension>]
type StringExtensions () =
    
    [<Extension>]
    static member inline Turncate(str : string, maxLength : int, suffix : string) =
        if String.IsNullOrEmpty(str) || str.Length <= maxLength then str
        else str[0..maxLength - 1] + suffix

    [<Extension>]
    static member inline Turncate(str : string, maxLength : int) =
        StringExtensions.Turncate(str, maxLength, "")

    [<Extension>]
    static member inline StripHtml input =
        Regex.Replace(input, "<.*?>", "")

    [<Extension>]
    static member inline RemoveDiacritics (input : string) =
        input
        |> (Encoding.GetEncoding 1251).GetBytes
        |> Encoding.ASCII.GetString

    [<Extension>]
    static member inline RemoveNonAlphaNum (input : string) =
        Regex.Replace(input, @"[^A-Za-z0-9]+", "")

    [<Extension>]
    static member inline TryConvertToInt32 (input : string) =
        let isNumeric, n = Int32.TryParse(input)
        if isNumeric then Some n
        else None

    [<Extension>]
    static member inline TryConvertToDecimal (input : string) =
        let isNumeric, n = Decimal.TryParse(input)
        if isNumeric then Some n
        else None

    [<Extension>]
    static member inline Webalize(input : string) =
        let rec link acc = function
        | ' ' :: t | '-' :: t | '_' :: t -> t |> link ('-' :: acc)
        | h :: t when Char.IsLetter h || Char.IsDigit h || h = '.' ->
            t |> link (Char.ToLower h :: acc)
        | _ :: t -> t |> link acc
        | [] -> acc
        input
        |> StringExtensions.RemoveDiacritics
        |> List.ofSeq |> link [] |> List.rev |> String.Concat

    [<Extension>]
    static member inline UpperCaseFirst(input : string) =
        if String.IsNullOrEmpty input then input
        else input[0..0].ToUpper() + input[1..]

    [<Extension>]
    static member inline LowerCaseFirst(input : string) =
        if String.IsNullOrEmpty input then input
        else input[0..0].ToLower() + input[1..]

    [<Extension>]
    static member inline ToLines(input : string) =
        Regex.Split(input, "\r\n|\r|\n")

    [<Extension>]
    static member inline IsNumber(input : string) =
        input |> Decimal.TryParse |> fst

    [<Extension>]
    static member inline IsIntNumber(input : string) =
        input |> Int64.TryParse |> fst

    [<Extension>]
    static member inline ReplaceAll(input : string, replacements : list<string * string>) =
        let rec replaceAll (replacements : list<string * string>) (stringBuilder : StringBuilder) =
            match replacements with
            | [] -> stringBuilder.ToString()
            | (oldValue, newValue) :: replacements ->
                replaceAll replacements (stringBuilder.Replace(oldValue, newValue))
        input
        |> StringBuilder
        |> replaceAll replacements


[<Extension>]
type DateTimeExtensions () =
    
    [<Extension>]
    static member inline ToISOString(dateTime : DateTime, ?isUniversal : bool) =
        let isUniversal = defaultArg isUniversal false
        let isoDateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat
        if isUniversal
        then isoDateTimeFormat.UniversalSortableDateTimePattern
        else isoDateTimeFormat.SortableDateTimePattern
        |> dateTime.ToString