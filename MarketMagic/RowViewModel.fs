namespace MarketMagic

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.ComponentModel
open System.Runtime.CompilerServices
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

type RowViewModelProps = {
    Cells: string[]
    IsNew : bool
    IsMarked : bool
}

type RowViewModel(props : RowViewModelProps) =
    inherit BasicViewModel()

    let mutable cellValues = props.Cells
    let mutable isNew = props.IsNew
    let mutable isMarked = props.IsMarked

    member self.Id
        with get() = Guid.NewGuid()

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

    member self.IsMarked
        with get() = isMarked
        and set(value) =
            if isMarked <> value then
                isMarked <- value
                self.OnPropertyChanged(nameof self.IsMarked)

    member private self.ToArray() = cellValues

    static member internal op_Explicit(row : RowViewModel) : string[] = row.ToArray()

    static member New(columnLength : int) =
        RowViewModel({
            Cells = Array.create columnLength ""
            IsNew = true
            IsMarked = false
        })

    static member New(columns : string seq) =
        RowViewModel.New(Seq.length columns)

    new(cells : string[]) =
        RowViewModel({
            Cells = cells
            IsNew = false
            IsMarked = false
        })

    new(rowViewModel : RowViewModel) =
        RowViewModel({
            Cells = rowViewModel |> RowViewModel.op_Explicit
            IsNew = rowViewModel.IsNew
            IsMarked = rowViewModel.IsMarked
        })