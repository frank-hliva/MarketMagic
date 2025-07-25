namespace MarketMagic

open Avalonia
open Avalonia.Controls
open Avalonia.Data
open Avalonia.Markup.Xaml
open System.Collections.Specialized
open System.Collections.ObjectModel
open System.Timers
open System.Threading
open Avalonia.Threading
open Lime.Timing
open MarketMagic.Ebay.Api

type MainWindow () as this = 
    inherit Window ()

    let viewModel = TableViewModel()
    let mutable dataGrid: DataGrid = null

    do
        this.InitializeComponent()
        this.SetupDataGrid()
        this.LoadData()

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        this.DataContext <- viewModel
        AvaloniaXamlLoader.Load(this)
        dataGrid <- this.FindControl<DataGrid>("UploadTable")

    member private this.SetupDataGrid() =
        viewModel.PropertyChanged.Add(fun args ->
            match args.PropertyName with
            | "Columns" -> this.UpdateDataGridColumns()
            | _ -> ()
        )

    member private this.UpdateDataGridColumns() =
        dataGrid.Columns.Clear()
        for i in 0 .. viewModel.Columns.Count - 1 do
            dataGrid.Columns.Add(
                DataGridTextColumn(
                    Header = viewModel.Columns[i],
                    Binding = Binding($"[{i}]")
                )
            )

    member private this.UpdateDataGridCells() =
        dataGrid.ItemsSource <- viewModel.Cells
        ()

    member private this.LoadData() =
        let upload = UploadTemplate.load @"C:/Workspace/MarketMagic/Engine/data/template.csv"
        let columns = UploadTemplate.fetch ()

        let columns = [| "A"; "B"; "C" |]
        let cells = [|
            [| "1"; "2"; "3" |]
            [| "4"; "5"; "6" |]
        |]
        
        viewModel.SetData(columns, cells)
