namespace MarketMagic

open System
open System.Threading
open Avalonia
open Avalonia.Controls
open Avalonia.Data
open Avalonia.Markup.Xaml
open Avalonia.Threading
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Platform.Storage
open Lime
open MarketMagic
open Avalonia.Markup.Xaml.Templates
open Avalonia.Controls.Templates

type WindowConfig(
    appConfig : AppConfig,
    uploadTemplateConfig : UploadTemplateConfig,
    moneyDocumentConfig : MoneyDocumentConfig
) =
    member self.AppConfig = appConfig

    member self.TabIndex
        with get () = appConfig.GetOr<int>("Window.TabIndex", 0)
        and set (value : int) =
            if not <| appConfig.TrySet("Window.TabIndex", value) then
                failwith "Failed to change Tab.Index configuration"

    member self.UploadTemplate = uploadTemplateConfig

    member self.MoneyDocument = moneyDocumentConfig

    member self.State
        with get () =
            appConfig.GetOr("Window.State", int WindowState.Normal)
            |> enum<WindowState>
        and set (value : WindowState) =
            if not <| appConfig.TrySet("Window.State", int value) then
                failwith "Failed to change Window.State configuration"

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

and MoneyDocumentConfig(appConfig : AppConfig) =
    let valueChanged = Event<unit>()

    member self.ValueChanged = valueChanged.Publish

    member self.AppConfig = appConfig

    member self.DocumentPath
        with get() = appConfig.GetOr("MoneyDocument.Document.Path", "")
        and set(value : string) =
            if appConfig.TrySet("MoneyDocument.Document.Path", value) then
                valueChanged.Trigger()

and WindowViewModel(
    windowConfig : WindowConfig,
    uploadTemplateConfig : UploadTemplateConfig,
    moneyDocumentConfig : MoneyDocumentConfig,
    uploadTableViewModel : TableViewModel,
    moneyTableViewModel : MoneyTableViewModel
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

    member self.UploadTable = uploadTableViewModel

    member self.MoneyTable = moneyTableViewModel

    member self.TabIndex
        with get() = windowConfig.TabIndex
        and set(value : int) = windowConfig.TabIndex <- value

and MainWindow (
    windowViewModel : WindowViewModel,
    uploadTemplateManager : Ebay.UploadTemplateManager,
    moneyDocumentManager : Money.MoneyDocumentManager,
    windowConfig : WindowConfig
) as self = 
    inherit Window ()

    let mutable uploadTableDataGrid : DataGrid = null
    let mutable moneyDocumentDataGrid : DataGrid = null

    let displayDataInTable() =
        uploadTemplateManager.Fetch().Data
        |> windowViewModel.UploadTable.SetData
        uploadTableDataGrid.Focus() |> ignore

    let displayMoneyDataInTable() =
        moneyDocumentManager.Fetch().Data
        |> windowViewModel.MoneyTable.SetData

        match windowViewModel.MoneyTable.TryExportToDataTable() with
        | Ok dataTable ->
            let sumResult = dataTable |> moneyDocumentManager.Sum
            windowViewModel.MoneyTable.Sum <- (
                if sumResult.Success
                then String.Format("{0:N2} €", sumResult.Value)
                else "#,## €"
            )
        | Error message ->
            ()
        moneyDocumentDataGrid.Focus() |> ignore

    let showError (msg : string) = 
        Dialogs.showErrorU msg self

    let showFileNotFound (path : string) = 
        showError $"The file \"{path}\" was not found."

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

    let showFailedToLoadMoneyDocument_invalidPath (path : string) = 
        showError $"Failed to load money document.\nThe file \"{path}\" was not found."

    let showErrorResponse (moneyDocument_loadInfo : CommandMessageResponse) =
        showError $"{moneyDocument_loadInfo.Error}\n{moneyDocument_loadInfo.InternalError}"

    let processError (moneyDocument_loadInfo : CommandMessageResponse) = task {
        do! showErrorResponse moneyDocument_loadInfo
        windowConfig.MoneyDocument.DocumentPath <- ""
        displayMoneyDataInTable()
    }

    let tryPickFileToOpen (title : string) (fileTypeTitle : string) = async {
        match! [
            FilePickerFileType(fileTypeTitle, Patterns = [| "*.csv" |])
            FilePickerFileTypes.All
        ] |> Dialogs.Pick.filesToOpen self {| title = title; allowMultiple = false |} with
        | [] -> return None
        | file :: _ ->
            return Some file.Path.LocalPath
    }

    let tryPickFileToSave (title : string) (fileTypeTitle : string) = async {
        match! [
            FilePickerFileType(fileTypeTitle, Patterns = [| "*.csv" |])
            FilePickerFileTypes.All
        ] |> Dialogs.Pick.filesToSave self {| title = title |} with
        | [] -> return None
        | file :: _ -> return Some file.Path.LocalPath
    }

    let tryPickFileToLoadUploadTemplate () = tryPickFileToOpen "Open template" "Upload templates (*.csv)"
    let tryPickFileToLoadDocument () = tryPickFileToOpen "Open document" "Documents (*.csv)"
    let tryPickFileToSaveDocument () = tryPickFileToSave "Save document" "Documents (*.csv)"

    let rec saveDocumentToFile (path : string) = task {
        match windowViewModel.UploadTable.TryExportToDataTable() with
        | Ok uploadDataTable ->
            let result = uploadTemplateManager.Save(path, uploadDataTable)
            if result.Success
            then windowConfig.UploadTemplate.DocumentPath <- path
            else do! showErrorResponse result
        | Error errMsg -> do! Dialogs.showErrorU errMsg self
    }

    and saveDocument () = task {
        let path = windowConfig.UploadTemplate.DocumentPath
        if IO.File.Exists path then do! saveDocumentToFile path
        else do! saveAsDocument ()
    }

    and saveAsDocument () = task {
        match! tryPickFileToSaveDocument() with
        | Some path -> do! saveDocumentToFile path
        | _ -> ()
    }

    let rec saveMoneyDocumentToFile (path : string) = task {
        match windowViewModel.MoneyTable.TryExportToDataTable() with
        | Ok moneyDataTable ->
            let result = moneyDocumentManager.Save(path, moneyDataTable)
            if result.Success
            then windowConfig.MoneyDocument.DocumentPath <- path
            else do! showErrorResponse result
        | Error errMsg -> do! Dialogs.showErrorU errMsg self
    }

    and saveMoneyDocument () = task {
        let path = windowConfig.MoneyDocument.DocumentPath
        if IO.File.Exists path then do! saveMoneyDocumentToFile path
        else do! saveAsMoneyDocument ()
    }

    and saveAsMoneyDocument () = task {
        match! tryPickFileToSaveDocument() with
        | Some path -> do! saveMoneyDocumentToFile path
        | _ -> ()
    }

    do
        self.InitializeComponent()
        self.SetupDataGrids()
        self.Opened.Add(self.Window_Opened)
        self.Closing.Add(self.Window_Closing)

    member private self.InitializeComponent() =
#if DEBUG
        self.AttachDevTools()
#endif
        self.DataContext <- windowViewModel
        AvaloniaXamlLoader.Load(self)
        uploadTableDataGrid <- self.FindControl<DataGrid>("UploadTable")
        moneyDocumentDataGrid <- self.FindControl<DataGrid>("MoneyTable")

    member private self.SetupDataGrids() =
        [
            uploadTableDataGrid => windowViewModel.UploadTable
            moneyDocumentDataGrid => windowViewModel.MoneyTable
        ]
        |> List.iter(
            fun (dataGrid, viewModel) ->
                viewModel
                |> unbox<TableViewModel>
                |> dataGrid.Setup
        )

    member private self.Window_Opened(event : EventArgs) =
        self.WindowState <- windowConfig.State

        self.TryLoadTemplateWithDocument()
        |> Async.AwaitTask
        |> Async.StartImmediate

        self.TryLoadMoneyDocument()
        |> Async.AwaitTask
        |> Async.StartImmediate

    member private self.Window_Closing(event : EventArgs) =
        windowConfig.State <- self.WindowState

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
                        do! showErrorResponse document_loadInfo
                        windowConfig.UploadTemplate.DocumentPath <- ""
                        displayDataInTable()
                | documentPath when not <| String.IsNullOrWhiteSpace(documentPath) ->
                    do! showFailedToLoadDocument_invalidPath documentPath
                    displayDataInTable()
                | _ -> ()
            else
                do! showErrorResponse uploadTemplate_loadInfo
                windowConfig.UploadTemplate.SourcePath <- ""
                windowConfig.UploadTemplate.DocumentPath <- ""
                displayDataInTable()
        | uploadTemplatePath ->
            do! showFailedToLoadUploadTemplate_invalidPath uploadTemplatePath
    }

    (* Upload Template *)
    member private self.LoadUploadTemplateButton_Click(sender : obj, event : RoutedEventArgs) =
        task {
            match! tryPickFileToLoadUploadTemplate() with
            | Some path ->
                windowConfig.UploadTemplate.SourcePath <- path
                windowConfig.UploadTemplate.DocumentPath <- ""
                self.TryLoadTemplateWithDocument() |> ignore
            | _ -> ()
        } |> ignore

    member private self.LoadDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        task {
            match! tryPickFileToLoadDocument() with
            | Some path ->
                windowConfig.UploadTemplate.DocumentPath <- path
                self.TryLoadTemplateWithDocument() |> ignore
            | _ -> ()
        } |> ignore

    member private self.SaveDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        saveDocument () |> ignore

    member private self.SaveAsDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        saveAsDocument () |> ignore

    member private self.DeleteRowsButton_Click(sender : obj, event : RoutedEventArgs) =
        windowViewModel.UploadTable.DeleteSelected()

    (* Money Document *)
    member private self.TryLoadMoneyDocument() = task {
        match windowConfig.MoneyDocument.DocumentPath with
        | "" | null ->
            let moneyDocument_loadInfo = moneyDocumentManager.New()
            if moneyDocument_loadInfo.Success
            then displayMoneyDataInTable()
            else do! processError moneyDocument_loadInfo
        | moneyDocumentPath when IO.Path.Exists(moneyDocumentPath) ->
            let moneyDocument_loadInfo = moneyDocumentManager.Load moneyDocumentPath
            if moneyDocument_loadInfo.Success
            then displayMoneyDataInTable()
            else do! processError moneyDocument_loadInfo
        | moneyDocumentPath ->
            do! showFailedToLoadMoneyDocument_invalidPath moneyDocumentPath
    }

    member private self.NewMoneyDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        windowConfig.MoneyDocument.DocumentPath <- ""
        self.TryLoadMoneyDocument() |> ignore
        ()

    member private self.LoadMoneyDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        task {
            match! tryPickFileToLoadDocument() with
            | Some path ->
                windowConfig.MoneyDocument.DocumentPath <- path
                self.TryLoadMoneyDocument() |> ignore
            | _ -> ()
        } |> ignore

    member private self.SaveMoneyDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        saveMoneyDocument () |> ignore

    member private self.SaveAsMoneyDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        saveAsMoneyDocument () |> ignore

    member private self.DeleteMoneyDocumentRowsButton_Click(sender : obj, event : RoutedEventArgs) =
        windowViewModel.MoneyTable.DeleteSelected()