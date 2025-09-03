namespace MarketMagic

open System
open System.Collections.ObjectModel
open System.ComponentModel

type TableViewModel() =
    inherit BasicViewModel()

    let mutable dataTable : DataTable option = None
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

    member self.DataTable
        with get() = dataTable
        and set(value) =
            dataTable <- value
            self.OnPropertyChanged(nameof self.DataTable)

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

    member self.SetData(dataTable : DataTable) =
        match box dataTable with
        | :? DataTable as dataTable ->
            self.DataTable <- Some dataTable
            self.Columns <- ObservableCollection<string>(dataTable.columns)
            self.Cells <-
                dataTable.cells
                |> Cells.toObservable
                |> withEmptyRow
        | _ ->
            self.DataTable <- None
            self.Columns <- ObservableCollection<string>()
            self.Cells <-
                ObservableCollection<RowViewModel>()
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

    member self.RemoveLastEmptyRow() =
        self.Cells <- removeLastEmptyRow self.Cells

    member self.TryExportToDataTable() : Result<DataTable, string> =
        match self.DataTable with
        | Some dataTable ->
            self.CanSave <- false
            { dataTable with
                columns = self.Columns |> List.ofSeq
                cells =
                    self.Cells
                    |> ObservableCollection<_>
                    |> removeLastEmptyRow
                    |> Cells.ofObservable self.Columns
            } |> Ok
        | _ -> Error "The table has not been loaded."