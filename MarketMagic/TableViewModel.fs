namespace rec MarketMagic

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
    
    let mutable uploadDataTable : Ebay.UploadDataTable option = None
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

    member self.UploadDataTable 
        with get() = uploadDataTable
        and set(value) = 
            uploadDataTable <- value
            self.OnPropertyChanged("UploadDataTable")

    member self.SetData(uploadDataTable : Ebay.UploadDataTable) =
        self.UploadDataTable <- Some uploadDataTable
        self.Columns <- ObservableCollection<string>(uploadDataTable.columns)
        self.Cells <- uploadDataTable.cells |> Cells.toObservable

    member self.TryExportToUploadDataTable() : Result<Ebay.UploadDataTable, string> =
        match self.UploadDataTable with
        | Some uploadDataTable ->
            { uploadDataTable with
                columns = self.Columns |> List.ofSeq
                cells = self.Cells |> Cells.ofObservable self.Columns
            } |> Ok
        | _ -> Error "The upload template has not been loaded."
    
module Cells =
    let toObservable (cells : string array2d) =
        let rowCount = cells.GetLength(0)
        let columnCount = cells.GetLength(1)
        [for y in 0 .. rowCount - 1 ->
            [| for x in 0 .. columnCount - 1 -> cells[y, x] |]
            |> RowViewModel
        ] |> ObservableCollection

    let ofObservable (columns : string ObservableCollection) (observableCells : RowViewModel ObservableCollection) = 
        let rowList = Seq.toList observableCells
        let rowCount = List.length rowList
        Array2D.init rowCount columns.Count (fun y x -> rowList[y][x])