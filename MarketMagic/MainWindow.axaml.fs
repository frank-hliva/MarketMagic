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
open System.ComponentModel
open System.Diagnostics

type WindowConfig(appConfig : AppConfig, uploadTemplateConfig : UploadTemplateConfig) =

    member self.AppConfig = appConfig

    member self.UploadTemplate = uploadTemplateConfig

and UploadTemplateConfig(appConfig : AppConfig) =
    let valueChanged = Event<unit>()

    member self.ValueChanged = valueChanged.Publish

    member self.AppConfig = appConfig

    member self.ExportedDataPath
        with get() = appConfig.GetOr("UploadTemplate.ExportedData.Path", "")
        and set(value : string) =
            if appConfig.TrySet("UploadTemplate.ExportedData.Path", value) then
                valueChanged.Trigger()

    member self.SourcePath
        with get() = appConfig.GetOr("UploadTemplate.Source.Path", "")
        and set(value : string) =
            if appConfig.TrySet("UploadTemplate.Source.Path", value) then
                valueChanged.Trigger()

and WindowViewModel(
    uploadTemplateConfig: UploadTemplateConfig,
    tableViewModel: TableViewModel
) as self =
    inherit BasicViewModel()

    let applyIfNotEmpty (filter : string -> string) = function
    | (null | "") as value -> value
    | input -> filter(input)

    let renderTemplateSource = applyIfNotEmpty <| sprintf "(%s)"
    let extractFileName = applyIfNotEmpty <| IO.Path.GetFileName

    let mutable title = ""

    do
        self.UpdateTitle()
        uploadTemplateConfig.ValueChanged.Add(self.UpdateTitle)

    member self.Title
        with get() = title
        and set(value) =
            title <- value
            self.OnPropertyChanged("Title")

    member self.UpdateTitle() =
        let path = extractFileName uploadTemplateConfig.ExportedDataPath
        let templateSourcePath = extractFileName uploadTemplateConfig.SourcePath
        let subtitle = [path; renderTemplateSource templateSourcePath] |> String.concat " "
        self.Title <- $"MarketMagic: {subtitle}"

    member self.Table = tableViewModel

and MainWindow (
    windowViewModel : WindowViewModel,
    uploadTemplateManager : UploadTemplateManager,
    windowConfig : WindowConfig
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
        let response = uploadTemplateManager.Fetch()
        windowViewModel.Table.SetData(response.Data)

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
        self.DataContext <- windowViewModel
        AvaloniaXamlLoader.Load(self)
        dataGrid <- self.FindControl<DataGrid>("UploadTable")

    member private self.SetupDataGrid() =
        windowViewModel.Table.PropertyChanged.Add(fun args ->
            match args.PropertyName with
            | "Columns" -> self.UpdateDataGridColumns()
            | _ -> ()
        )

    member private self.UpdateDataGridColumns() =
        dataGrid.Columns.Clear()
        for i in 0 .. windowViewModel.Table.Columns.Count - 1 do
            dataGrid.Columns.Add(
                DataGridTextColumn(
                    Header = windowViewModel.Table.Columns[i],
                    Binding = Binding($"[{i}]")
                )
            )

    member private self.HandleWindowOpened(event : EventArgs) =
        self.LoadData()
        |> Async.AwaitTask
        |> Async.StartImmediate

    member private self.UpdateDataGridCells() =
        dataGrid.ItemsSource <- windowViewModel.Table.Cells
        ()

    member private self.LoadData() = task {
        match windowConfig.UploadTemplate.SourcePath with
        | "" -> do! showFailedToLoadUploadTemplate()
        | uploadTemplatePath when IO.Path.Exists(uploadTemplatePath) ->
            if (uploadTemplateManager.Load uploadTemplatePath).Success then
                match windowConfig.UploadTemplate.ExportedDataPath with
                | "" -> displayDataInTable()
                | exportedDataPath when IO.Path.Exists(exportedDataPath) ->
                    if (uploadTemplateManager.AddExportedData exportedDataPath).Success then
                        displayDataInTable()
                    else
                        do! showFailedToLoadExportedData()
                        displayDataInTable()
                | exportedDataPath when not <| String.IsNullOrWhiteSpace(exportedDataPath) ->
                    do! showFailedToLoadExportedData_invalidPath(exportedDataPath)
                    displayDataInTable()
                | _ -> ()
            else do! showFailedToLoadUploadTemplate()
        | uploadTemplatePath -> do! showFailedToLoadUploadTemplate_invalidPath(uploadTemplatePath)
    }

    member private self.OpenUploadTemplateButton_Click(sender: obj, event: RoutedEventArgs) =
        task {
            match! tryPickFileToOpen() with
            | Some path ->
                windowConfig.UploadTemplate.SourcePath <- path
                windowConfig.UploadTemplate.ExportedDataPath <- ""
                self.LoadData() |> ignore
            | _ -> ()
        } |> ignore

    member private self.InsertDataButton_Click(sender: obj, event: RoutedEventArgs) =
        task {
            match! tryPickFileToOpen() with
            | Some path ->
                windowConfig.UploadTemplate.ExportedDataPath <- path
                self.LoadData() |> ignore
            | _ -> ()
        } |> ignore

    member private self.SaveButton_Click(sender: obj, event: RoutedEventArgs) =
        task {
            match! tryPickFileToSave() with
            | Some path ->
                match windowViewModel.Table.TryExportToUploadDataTable() with
                | Ok uploadDataTable ->
                    if uploadTemplateManager.Save(path, uploadDataTable).Success
                    then windowConfig.UploadTemplate.ExportedDataPath <- path
                    else do! showUploadTemplateFailedToSave ()
                | Error errMsg -> do! Dialogs.Unit.showError errMsg self
            | _ -> ()
        } |> ignore
