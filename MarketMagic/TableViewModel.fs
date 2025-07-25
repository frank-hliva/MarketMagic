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

    member this.Columns 
        with get() = columns
        and set(value) = 
            columns <- value
            this.OnPropertyChanged("Columns")
    
    member this.Cells 
        with get() = cells
        and set(value) = 
            cells <- value
            this.OnPropertyChanged("Cells")

    member this.SetData(columns: string list, cells: string[][]) =
        this.Columns <- ObservableCollection<string>(columns)
        this.Cells <- cells |> Seq.map RowViewModel |> ObservableCollection<RowViewModel>
        ()

