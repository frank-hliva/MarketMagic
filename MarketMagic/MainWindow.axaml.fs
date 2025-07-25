namespace MarketMagic

open System.Collections.Specialized
open System.Collections.ObjectModel
open System.Timers
open System.Threading
open Avalonia
open Avalonia.Controls
open Avalonia.Data
open Avalonia.Markup.Xaml
open Avalonia.Threading
open Lime.Timing
open MarketMagic
open MarketMagic.Ebay
open MsBox.Avalonia
open MsBox.Avalonia.Enums

type MainWindow () as this = 
    inherit Window ()

    let viewModel = TableViewModel()
    let mutable dataGrid: DataGrid = null

    do
        this.InitializeComponent()
        this.SetupDataGrid()
        this.Opened.Add(this.HandleWindowOpened)

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

    member private this.HandleWindowOpened(_) =
        this.LoadData()
        |> Async.AwaitTask
        |> Async.StartImmediate

    member private this.UpdateDataGridCells() =
        dataGrid.ItemsSource <- viewModel.Cells
        ()

    member private this.LoadData() = task {
        if (UploadTemplate.load @"C:/Workspace/MarketMagic/Engine/data/template.csv").Success then
            if (UploadTemplate.addExportedData @"C:/Workspace/MarketMagic/Engine/data/active.csv").Success then
                let response = UploadTemplate.fetch ()
                viewModel.SetData(
                    response.Data.columns,
                    response.Data.cells
                )
            else
                let! _ = this |> Dialogs.showError "Failed to load upload template."
                ()
        else
            let! _ = this |> Dialogs.showError "Failed to load upload template." 
            ()
    }


