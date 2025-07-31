namespace MarketMagic

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
open System.Collections.Specialized
open System.Collections.ObjectModel
open System.Timers
open System.Threading
open Avalonia.Platform.Storage
open Tomlyn.Model

type MainWindow (
    viewModel : TableViewModel,
    uploadTemplate : UploadTemplate,
    appConfig: AppConfig
) as self = 
    inherit Window ()

    let mutable dataGrid: DataGrid = null

    let showFailedToLoadUploadTemplate () = 
        Dialogs.Unit.showError "Failed to load upload template." self

    let showFailedToLoadUploadTemplate_invalidPath (path : string) = 
        Dialogs.Unit.showError $"Failed to load upload template.\nThe file \"{path}\" was not found." self

    let showFailedToLoadExportedData () = 
        Dialogs.Unit.showError "Failed to load exported data." self

    let showFailedToLoadExportedData_invalidPath (path : string) = 
        Dialogs.Unit.showError $"Failed to load exported data.\nThe file \"{path}\" was not found." self

    let showUploadTemplateFailedToChange () = 
        Dialogs.Unit.showError "Upload template failed to change." self

    let showUploadTemplateFailedToSave () = 
        Dialogs.Unit.showError "Upload template failed to save." self

    let displayDataInTable() =
        let response = uploadTemplate.Fetch()
        viewModel.SetData(response.Data)

    let tryPickFileToOpen () = async {            
        match! self.StorageProvider.OpenFilePickerAsync(
                FilePickerOpenOptions(
                    Title = "Select file",
                    AllowMultiple = false,
                    FileTypeFilter = [
                        FilePickerFileType("Upload templates (*.csv)", Patterns = [| "*.csv" |])
                        FilePickerFileTypes.All
                    ]
                )
            ) |> Async.AwaitTask with
        | null -> return None
        | files ->
            match List.ofSeq files with
            | [] -> return None
            | file :: _ ->
                return Some file.Path.LocalPath
    }

    let tryPickFileToSave () = async {
        match! self.StorageProvider.SaveFilePickerAsync(
                FilePickerSaveOptions(
                    Title = "Save file",
                    FileTypeChoices = [
                        FilePickerFileType("Upload templates (*.csv)", Patterns = [| "*.csv" |])
                        FilePickerFileTypes.All
                    ]
                )
            ) |> Async.AwaitTask with
        | null -> return None
        | file -> return Some file.Path.LocalPath
    }

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
        match appConfig.TryGet<string>("UploadTemplate.Source.Path") with
        | Some uploadTemplatePath when IO.Path.Exists(uploadTemplatePath) ->
            if (uploadTemplate.Load uploadTemplatePath).Success then
                match appConfig.TryGet<string>("UploadTemplate.ExportedData.Path") with
                | Some exportedDataPath when IO.Path.Exists(exportedDataPath) ->
                    if (uploadTemplate.AddExportedData exportedDataPath).Success then
                        displayDataInTable()
                    else
                        do! showFailedToLoadExportedData()
                        displayDataInTable()
                | Some exportedDataPath when not <| String.IsNullOrWhiteSpace(exportedDataPath) ->
                    do! showFailedToLoadExportedData_invalidPath(exportedDataPath)
                    displayDataInTable()
                | _ -> displayDataInTable()
            else do! showFailedToLoadUploadTemplate()
        | Some uploadTemplatePath -> do! showFailedToLoadUploadTemplate_invalidPath(uploadTemplatePath)
        | _ -> do! showFailedToLoadUploadTemplate()
    }

    member private self.OpenUploadTemplateButton_Click(sender: obj, event: RoutedEventArgs) =
        task {
            match! tryPickFileToOpen() with
            | Some path ->
                if appConfig.TrySet("UploadTemplate.Source.Path", path) then
                    appConfig.TrySet("UploadTemplate.ExportedData.Path", null) |> ignore
                    self.LoadData() |> ignore
                else do! showUploadTemplateFailedToChange ()
            | _ -> ()
        } |> ignore

    member private self.InsertDataButton_Click(sender: obj, event: RoutedEventArgs) =
        task {
            match! tryPickFileToOpen() with
            | Some path ->
                if appConfig.TrySet("UploadTemplate.ExportedData.Path", path)
                then self.LoadData() |> ignore
                else do! showUploadTemplateFailedToChange ()
            | _ -> ()
        } |> ignore

    member private self.SaveButton_Click(sender: obj, event: RoutedEventArgs) =
        task {
            match! tryPickFileToSave() with
            | Some path ->
                match viewModel.TryExportToUploadDataTable() with
                | Ok uploadDataTable ->
                    if uploadTemplate.Save(path, uploadDataTable).Success
                    then appConfig.TrySet("UploadTemplate.ExportedData.Path", path) |> ignore
                    else do! showUploadTemplateFailedToSave ()
                | Error errMsg -> do! Dialogs.Unit.showError errMsg self
            | _ -> ()
        } |> ignore