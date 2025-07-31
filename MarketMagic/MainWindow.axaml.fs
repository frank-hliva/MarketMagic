namespace MarketMagic

open Avalonia
open Avalonia.Controls
open Avalonia.Data
open Avalonia.Markup.Xaml
open Avalonia.Threading
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
open Lime
open Dialogs

type WindowConfig(appConfig : AppConfig, uploadTemplateConfig : UploadTemplateConfig) =

    member self.AppConfig = appConfig

    member self.UploadTemplate = uploadTemplateConfig

and UploadTemplateConfig(appConfig : AppConfig) =
    let valueChanged = Event<unit>()

    member self.ValueChanged = valueChanged.Publish

    member self.AppConfig = appConfig

    member self.DocumentPath
        with get() = appConfig.GetOr("UploadTemplate.Document.Path", "")
        and set(value : string) =
            if appConfig.TrySet("UploadTemplate.Document.Path", value) then
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

    let renderTemplateSource = applyIfNotEmpty <| sprintf "<Template: %s>"
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
        let path = extractFileName uploadTemplateConfig.DocumentPath
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

    let showError (msg : string) = 
        Dialogs.showErrorU msg self

    let showFailedToLoadUploadTemplate () = 
        showError "Failed to load upload template."

    let showFailedToLoadUploadTemplate_invalidPath (path : string) = 
        showError $"Failed to load upload template.\nThe file \"{path}\" was not found."

    let showFailedToLoadDocument () = 
        showError "Failed to load exported data."

    let showFailedToLoadDocument_invalidPath (path : string) = 
        showError $"Failed to load exported data.\nThe file \"{path}\" was not found."

    let showUploadTemplateFailedToChange () = 
        showError "Upload template failed to change."

    let showUploadTemplateFailedToSave () = 
        showError "Upload template failed to save."

    let displayDataInTable() =
        let response = uploadTemplateManager.Fetch()
        windowViewModel.Table.SetData(response.Data)

    let tryPickFileToOpen (title : string) (fileTypeTitle : string) = async {
        match! [
            FilePickerFileType(fileTypeTitle, Patterns = [| "*.csv" |])
            FilePickerFileTypes.All
        ] |> Pick.filesToOpen self {| title = title; allowMultiple = false |} with
        | [] -> return None
        | file :: _ ->
            return Some file.Path.LocalPath
    }

    let tryPickFileToSave (title : string) (fileTypeTitle : string) = async {
        match! [
            FilePickerFileType(fileTypeTitle, Patterns = [| "*.csv" |])
            FilePickerFileTypes.All
        ] |> Pick.filesToSave self {| title = title |} with
        | [] -> return None
        | file :: _ -> return Some file.Path.LocalPath
    }

    let tryPickFileToLoadUploadTemplate () = tryPickFileToOpen "Open template" "Upload templates (*.csv)"
    let tryPickFileToLoadDocument () = tryPickFileToOpen "Open document" "Documents (*.csv)"
    let tryPickFileToSaveDocument () = tryPickFileToOpen "Save document" "Documents (*.csv)"

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
        self.TryLoadTemplateWithDocument()
        |> Async.AwaitTask
        |> Async.StartImmediate

    member private self.UpdateDataGridCells() =
        dataGrid.ItemsSource <- windowViewModel.Table.Cells
        ()

    member private self.TryLoadTemplateWithDocument() = task {
        match windowConfig.UploadTemplate.SourcePath with
        | "" -> ()
        | uploadTemplatePath when IO.Path.Exists(uploadTemplatePath) ->
            let uploadTemplate_loadInfo = uploadTemplateManager.Load uploadTemplatePath
            if uploadTemplate_loadInfo.Success then
                match windowConfig.UploadTemplate.DocumentPath with
                | "" -> displayDataInTable()
                | documentPath when IO.Path.Exists(documentPath) ->
                    let document_loadInfo = uploadTemplateManager.LoadDocument documentPath
                    if document_loadInfo.Success then
                        displayDataInTable()
                    else
                        do! showError document_loadInfo.Error
                        windowConfig.UploadTemplate.DocumentPath <- ""
                        displayDataInTable()
                | documentPath when not <| String.IsNullOrWhiteSpace(documentPath) ->
                    do! showFailedToLoadDocument_invalidPath(documentPath)
                    displayDataInTable()
                | _ -> ()
            else
                do! showError uploadTemplate_loadInfo.Error
                windowConfig.UploadTemplate.SourcePath <- ""
                windowConfig.UploadTemplate.DocumentPath <- ""
                displayDataInTable()
        | uploadTemplatePath ->
            do! showFailedToLoadUploadTemplate_invalidPath(uploadTemplatePath)
    }

    member private self.LoadUploadTemplateButton_Click(sender: obj, event: RoutedEventArgs) =
        task {
            match! tryPickFileToLoadUploadTemplate() with
            | Some path ->
                windowConfig.UploadTemplate.SourcePath <- path
                windowConfig.UploadTemplate.DocumentPath <- ""
                self.TryLoadTemplateWithDocument() |> ignore
            | _ -> ()
        } |> ignore

    member private self.LoadDocumentButton_Click(sender: obj, event: RoutedEventArgs) =
        task {
            match! tryPickFileToLoadDocument() with
            | Some path ->
                windowConfig.UploadTemplate.DocumentPath <- path
                self.TryLoadTemplateWithDocument() |> ignore
            | _ -> ()
        } |> ignore

    member private self.SaveDocumentButton_Click(sender: obj, event: RoutedEventArgs) =
        task {
            match! tryPickFileToSaveDocument() with
            | Some path ->
                match windowViewModel.Table.TryExportToUploadDataTable() with
                | Ok uploadDataTable ->
                    if uploadTemplateManager.Save(path, uploadDataTable).Success
                    then windowConfig.UploadTemplate.DocumentPath <- path
                    else do! showUploadTemplateFailedToSave ()
                | Error errMsg -> do! Dialogs.showErrorU errMsg self
            | _ -> ()
        } |> ignore
