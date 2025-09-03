[<AutoOpen>]
module Lime.Helpers

open System
open System.IO

let (/+) (path1 : string) (path2 : string) = Path.Join(path1, path2)

let (=>) (key : 'a) (value : 'b) =
    key, (value :> obj)

let applyIfNotEmpty (filter : string -> string) = function
| (null | "") as value -> value
| input -> filter(input)