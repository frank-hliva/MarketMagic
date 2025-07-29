[<AutoOpen>]
module Lime.Ops

open System
open System.IO

let (/+) (path1 : string) (path2 : string) = Path.Join(path1, path2)