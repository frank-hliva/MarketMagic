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
open Avalonia.Interactivity
open System

type MainWindow (viewModel : TableViewModel, appConfigProvider : AppConfigProvider) as self = 
    inherit Window ()

    let mutable dataGrid: DataGrid = null

    do
        self.InitializeComponent()
        self.SetupDataGrid()
        self.Opened.Add(self.HandleWindowOpened)

    member private self.InitializeComponent() =
#if DEBUG
        self.AttachDevTools()
#endif
        self.DataContext <- viewModel
        AvaloniaXamlLoader.Load(self)
        dataGrid <- self.FindControl<DataGrid>("UploadTable")

    member private self.SetupDataGrid() =
        viewModel.PropertyChanged.Add(fun args ->
            match args.PropertyName with
            | "Columns" -> self.UpdateDataGridColumns()
            | _ -> ()
        )

    member private self.UpdateDataGridColumns() =
        dataGrid.Columns.Clear()
        for i in 0 .. viewModel.Columns.Count - 1 do
            dataGrid.Columns.Add(
                DataGridTextColumn(
                    Header = viewModel.Columns[i],
                    Binding = Binding($"[{i}]")
                )
            )

    member private self.HandleWindowOpened(event : EventArgs) =
        self.LoadData()
        |> Async.AwaitTask
        |> Async.StartImmediate

    member private self.UpdateDataGridCells() =
        dataGrid.ItemsSource <- viewModel.Cells
        ()

    member private self.LoadData() = task {
        if (UploadTemplate.load @"C:/Workspace/MarketMagic/Engine/data/template.csv").Success then
            if (UploadTemplate.addExportedData @"C:/Workspace/MarketMagic/Engine/data/active.csv").Success then
                let response = UploadTemplate.fetch ()
                viewModel.SetData(
                    response.Data.columns,
                    response.Data.cells
                )
            else
                let! _ = self |> Dialogs.showError "Failed to load upload template."
                ()
        else
            let! _ = self |> Dialogs.showError "Failed to load upload template." 
            ()
    }

    member private self.OpenButton_Click(sender: obj, event: RoutedEventArgs) =
        ()

    member private self.InsertButton_Click(sender: obj, event: RoutedEventArgs) =
        ()

    member private self.SaveButton_Click(sender: obj, event: RoutedEventArgs) =
        ()