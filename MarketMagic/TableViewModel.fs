namespace MarketMagic

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.ComponentModel
open System.Runtime.CompilerServices

type TableViewModel() =
    inherit BasicViewModel()
    
    let mutable columns = ObservableCollection<string>()
    let mutable cells = ObservableCollection<RowViewModel>()

    member self.Columns 
        with get() = columns
        and set(value) = 
            columns <- value
            self.OnPropertyChanged("Columns")
    
    member self.Cells 
        with get() = cells
        and set(value) = 
            cells <- value
            self.OnPropertyChanged("Cells")

    member self.SetData(columns: string list, cells: string[,]) =
        self.Columns <- ObservableCollection<string>(columns)
        let rowCount = cells.GetLength(0)
        let columnCount = cells.GetLength(1)
        self.Cells <-
            [for y in 0 .. rowCount - 1 ->
                [| for x in 0 .. columnCount - 1 -> cells[y, x] |]
                |> RowViewModel
            ] |> ObservableCollection