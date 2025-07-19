module Lime.Timing

open System
open System.Timers
open System.Threading
open Avalonia.Threading

let setTimeout (action: unit -> unit) (milliseconds: int) =
    new Timer((fun _ -> action()), null, milliseconds, Timeout.Infinite)

let setInterval (action: unit -> unit) (milliseconds: int) =
    new Timer((fun _ -> action()), null, milliseconds, milliseconds)

module UI =
    let setTimeout (action: unit -> unit) (milliseconds: int) =
        setTimeout (fun () -> Dispatcher.UIThread.Post(action) |> ignore) milliseconds

    let setInterval (action: unit -> unit) (milliseconds: int) =
        setInterval (fun () -> Dispatcher.UIThread.Post(action) |> ignore) milliseconds