namespace MarketMagic

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.ComponentModel
open System.Runtime.CompilerServices

type RowViewModel(props : {| cells: string[]; isNew : bool |}) =
    inherit BasicViewModel()

    let mutable cellValues = props.cells
    let mutable isNew = props.isNew

    member self.Item
        with get(i) = cellValues[i]
        and set(i) value =
            cellValues[i] <- value
            self.OnPropertyChanged($"Item[{i}]")
            self.IsNew <- false

    member self.IsNew
        with get() = isNew
        and private set(value) =
            if isNew <> value then
                isNew <- value
                self.OnPropertyChanged(nameof self.IsNew)

    static member New(columnLength : int) =
        RowViewModel({|
            cells = Array.create columnLength ""
            isNew = true
        |})

    static member New(columns : string seq) =
        RowViewModel.New(Seq.length columns)

    new(cells: string[]) =
        RowViewModel({|
            cells = cells
            isNew = false
        |})