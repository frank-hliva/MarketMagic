module MarketMagic.Cells

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

let toObservable (cells : string array2d) : RowViewModel ObservableCollection =
    let rowCount = cells.GetLength(0)
    let columnCount = cells.GetLength(1)
    [for y in 0 .. rowCount - 1 ->
        [| for x in 0 .. columnCount - 1 -> cells[y, x] |] |> RowViewModel
    ] |> ObservableCollection

let ofObservable (columns : string ObservableCollection) (observableCells : RowViewModel ObservableCollection) : string array2d = 
    let rowList = Seq.toList observableCells
    let rowCount = List.length rowList
    Array2D.init rowCount columns.Count (fun y x -> rowList[y][x])