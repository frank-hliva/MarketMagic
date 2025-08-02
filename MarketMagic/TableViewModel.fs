namespace rec MarketMagic

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.ComponentModel
open System.Runtime.CompilerServices
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

type TableViewModel() =
    inherit BasicViewModel()
    
    let mutable uploadDataTable : Ebay.UploadDataTable option = None
    let mutable columns = ObservableCollection<string>()
    let mutable cells = ObservableCollection<RowViewModel>()
    let mutable cellInfo = ""
    let mutable help = ""
    let mutable isInEditingMode = false
    let mutable canSave = false

    let rec withEmptyRow (observableRows : ObservableCollection<RowViewModel>) =
        let lastEmptyRow = RowViewModel.New(columns)
        registerRowChangedEvent lastEmptyRow
        observableRows.Add lastEmptyRow
        observableRows

    and registerRowChangedEvent (row : RowViewModel) =
        let handlerRef = ref None
        let handler = PropertyChangedEventHandler(fun _ args ->
            if args.PropertyName = "IsNew" && not row.IsNew then
                if Object.ReferenceEquals(row, cells[cells.Count - 1]) then
                    let newRow = RowViewModel.New(columns)
                    registerRowChangedEvent newRow
                    cells.Add(newRow)
                handlerRef.Value
                |> Option.iter row.PropertyChanged.RemoveHandler
        )
        handlerRef := Some handler
        row.PropertyChanged.AddHandler(handler)

    let removeLastEmptyRow (observableRows : ObservableCollection<RowViewModel>) =
        let rowViewModel = observableRows[observableRows.Count - 1]
        if rowViewModel.IsNew then
            observableRows.Remove(rowViewModel) |> ignore
        observableRows

    member self.UploadDataTable 
        with get() = uploadDataTable
        and set(value) = 
            uploadDataTable <- value
            self.OnPropertyChanged(nameof self.UploadDataTable)

    member self.Columns 
        with get() = columns
        and set(value) = 
            columns <- value
            self.OnPropertyChanged(nameof self.Columns)
    
    member self.Cells 
        with get() = cells
        and set(value) = 
            cells <- value
            self.OnPropertyChanged(nameof self.Cells)

    member self.CursorYXHelp 
        with get() = cellInfo
        and set(value) = 
            cellInfo <- value
            self.OnPropertyChanged(nameof self.CursorYXHelp)

    member self.KeyboardHelp 
        with get() = help
        and set(value) = 
            help <- value
            self.OnPropertyChanged(nameof self.KeyboardHelp)

    member self.IsInEditMode
        with get() = isInEditingMode
        and set(value) =
            if isInEditingMode <> value then
                isInEditingMode <- value
                canSave <- true
                self.OnPropertyChanged("IsInEditMode")
                self.OnPropertyChanged("CanSave")

    member self.CanSave
        with get() = canSave
        and set(value) =
            if canSave <> value then
                canSave <- value
                self.OnPropertyChanged("CanSave")

    member self.SetData(uploadDataTable : Ebay.UploadDataTable) =
        self.UploadDataTable <- Some uploadDataTable
        self.Columns <- ObservableCollection<string>(uploadDataTable.columns)
        self.Cells <-
            uploadDataTable.cells
            |> Cells.toObservable
            |> withEmptyRow
        self.CursorYXHelp <- ""
        self.KeyboardHelp <- ""
        self.CanSave <- false

    member self.DeleteSelected() =
        self.Cells <-
            self.Cells
            |> Seq.filter(fun cell -> not cell.IsMarked || cell.IsNew)
            |> ObservableCollection
        self.CanSave <- true

    member self.TryExportToUploadDataTable() : Result<Ebay.UploadDataTable, string> =
        match self.UploadDataTable with
        | Some uploadDataTable ->
            self.CanSave <- false
            { uploadDataTable with
                columns = self.Columns |> List.ofSeq
                cells =
                    self.Cells
                    |> ObservableCollection<_>
                    |> removeLastEmptyRow
                    |> Cells.ofObservable self.Columns
            } |> Ok
        | _ -> Error "The upload template has not been loaded."
    
module Cells =
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