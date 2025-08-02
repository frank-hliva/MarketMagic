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
open MarketMagic.Ebay
open Avalonia.Markup.Xaml.Templates
open Avalonia.Controls.Templates

type WindowConfig(appConfig : AppConfig, uploadTemplateConfig : UploadTemplateConfig) =

    member self.AppConfig = appConfig

    member self.UploadTemplate = uploadTemplateConfig

    member self.State
        with get() =
            appConfig.GetOr("Window.State", (int)WindowState.Normal)
            |> enum<WindowState>
        and set(value : WindowState) =
            if not <| appConfig.TrySet("Window.State", (int)value) then
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

    let displayDataInTable() =
        windowViewModel.Table.SetData <| uploadTemplateManager.Fetch().Data
        dataGrid.Focus() |> ignore

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

    let showUploadTemplateFailedToSave () = 
        showError "Upload template failed to save."

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

    let displayDataGridCellInfo(dataGrid : DataGrid) =
        let cellInfo, keyboardInfo = dataGrid.GetDataGridCellInfo(windowViewModel.Table.IsInEditMode)
        windowViewModel.Table.CursorYXHelp <- cellInfo
        windowViewModel.Table.KeyboardHelp <- keyboardInfo

    let rec saveDocumentToFile (path : string) = task {
        match windowViewModel.Table.TryExportToUploadDataTable() with
        | Ok uploadDataTable ->
            if uploadTemplateManager.Save(path, uploadDataTable).Success
            then windowConfig.UploadTemplate.DocumentPath <- path
            else do! showUploadTemplateFailedToSave ()
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

    do
        self.InitializeComponent()
        self.SetupDataGrid()
        self.Opened.Add(self.Window_Opened)
        self.Closing.Add(self.Window_Closing)

        dataGrid.BeginningEdit.Add(fun _ ->
            windowViewModel.Table.IsInEditMode <- true
            displayDataGridCellInfo <| dataGrid
        )
        dataGrid.CellEditEnded.Add(fun _ ->
            windowViewModel.Table.IsInEditMode <- false
            displayDataGridCellInfo <| dataGrid
        )

        dataGrid.AddHandler(
            InputElement.KeyDownEvent,
            EventHandler<KeyEventArgs>(
                fun sender event ->
                    self.UploadTableDataGrid_KeyDown(sender, event)
            ),
            RoutingStrategies.Tunnel
        )

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
        DataGridCheckBoxColumn(
            Header = "#",
            Binding = Binding("IsMarked"),
            Width = DataGridLength(40.0, DataGridLengthUnitType.Pixel)
        ) |> dataGrid.Columns.Add

        let enums =
            windowViewModel.Table.UploadDataTable
            |> Option.map _.enums
            |> Option.defaultValue Map.empty

        for i in 0 .. windowViewModel.Table.Columns.Count - 1 do
            let column = windowViewModel.Table.Columns[i]
            dataGrid.Columns.Add(
                match enums.TryFind column with
                | Some enum when not enum.IsEmpty ->
                    DataGridTemplateColumn(
                        Header = column,
                        CellTemplate = FuncDataTemplate(
                            typeof<RowViewModel>,
                            Func<obj, INameScope, Control>(fun item _ ->
                                let row = item :?> RowViewModel
                                TextBlock(
                                    Text = row[i],
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                                    Padding = Thickness(10.0, 0.0)
                                ) :> Control
                            ),
                            false
                        ),
                        CellEditingTemplate = FuncDataTemplate(
                            typeof<RowViewModel>,
                            Func<obj, INameScope, Control>(fun item _ ->
                                let row = item :?> RowViewModel
                                let autoCompleteBox = AutoCompleteBox(
                                    ItemsSource = enum,
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
                                )
                                autoCompleteBox.Bind(AutoCompleteBox.TextProperty, Binding($"[{i}]")) |> ignore
                                autoCompleteBox :> Control
                            ),
                            false
                        )
                    ) :> DataGridColumn
                | _ ->
                    DataGridTextColumn(
                        Header = column,
                        Binding = Binding($"[{i}]")
                    ) :> DataGridColumn
            )

    member private self.Window_Opened(event : EventArgs) =
        self.WindowState <- windowConfig.State
        self.TryLoadTemplateWithDocument()
        |> Async.AwaitTask
        |> Async.StartImmediate

    member private self.Window_Closing(event : EventArgs) =
        windowConfig.State <- self.WindowState

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

    member private self.SaveDocumentButton_Click(sender: obj, event: RoutedEventArgs) =
        saveDocument () |> ignore

    member private self.SaveAsDocumentButton_Click(sender: obj, event: RoutedEventArgs) =
        saveAsDocument () |> ignore

    member private self.UploadTableDataGrid_CurrentCellChanged(sender: obj, e: EventArgs) =
        sender
        :?> DataGrid
        |> displayDataGridCellInfo

    member this.UploadTableDataGrid_KeyDown(sender : obj, e : KeyEventArgs) =
        let dataGrid = sender :?> DataGrid
        if not dataGrid.IsReadOnly then
            match e.Key with
            | Key.Enter | Key.F2 | Key.Insert when not windowViewModel.Table.IsInEditMode ->
                if dataGrid.SelectedItem <> null && dataGrid.CurrentColumn <> null then
                    dataGrid.BeginEdit() |> ignore
                    e.Handled <- true
            | Key.Enter when e.KeyModifiers.HasFlag KeyModifiers.Shift ->
                e.Handled <- true
            | Key.Enter | Key.Tab | Key.Escape when windowViewModel.Table.IsInEditMode ->
                if dataGrid.SelectedItem <> null && dataGrid.CurrentColumn <> null then
                    dataGrid.CommitEdit() |> ignore
                    dataGrid.Reselect()
                    e.Handled <- true
            | Key.Delete when not dataGrid.IsReadOnly ->
                e.Handled <- true
            | _ -> ()

    member private self.DeleteRowsButton_Click(sender : obj, event : RoutedEventArgs) =
        windowViewModel.Table.DeleteSelected()