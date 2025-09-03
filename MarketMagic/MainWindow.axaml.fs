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
    uploadTableViewModel : UploadTableViewModel,
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

    let displayDataGridCellInfo(dataGrid : DataGrid) =
        let cellInfo, keyboardInfo = dataGrid.GetDataGridCellInfo(windowViewModel.UploadTable.IsInEditMode)
        windowViewModel.UploadTable.CursorYXHelp <- cellInfo
        windowViewModel.UploadTable.KeyboardHelp <- keyboardInfo

    let rec saveDocumentToFile (path : string) = task {
        match windowViewModel.UploadTable.TryExportToUploadDataTable() with
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

    do
        self.InitializeComponent()
        self.SetupDataGrid()
        self.Opened.Add(self.Window_Opened)
        self.Closing.Add(self.Window_Closing)

        uploadTableDataGrid.BeginningEdit.Add(fun _ ->
            windowViewModel.UploadTable.IsInEditMode <- true
            displayDataGridCellInfo <| uploadTableDataGrid
        )
        uploadTableDataGrid.CellEditEnded.Add(fun _ ->
            windowViewModel.UploadTable.IsInEditMode <- false
            displayDataGridCellInfo <| uploadTableDataGrid
        )

        uploadTableDataGrid.AddHandler(
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
        uploadTableDataGrid <- self.FindControl<DataGrid>("UploadTable")
        moneyDocumentDataGrid <- self.FindControl<DataGrid>("MoneyTable")

    member private self.SetupDataGrid() =
        windowViewModel.UploadTable.PropertyChanged.Add(fun args ->
            match args.PropertyName with
            | "Columns" -> self.SetupDataGridColumns()
            | _ -> ()
        )

    member private self.SetupDataGridColumns() =
        uploadTableDataGrid.Columns.Clear()
        DataGridCheckBoxColumn(
            Header = "#",
            Binding = Binding("IsMarked"),
            Width = DataGridLength(40.0, DataGridLengthUnitType.Pixel)
        ) |> uploadTableDataGrid.Columns.Add

        let enums =
            windowViewModel.UploadTable.UploadDataTable
            |> Option.map _.enums
            |> Option.defaultValue Map.empty

        for i in 0 .. windowViewModel.UploadTable.Columns.Count - 1 do
            let column = windowViewModel.UploadTable.Columns[i]
            uploadTableDataGrid.Columns.Add(
                match enums.TryFind column with
                | Some enumInfo when not enumInfo.values.IsEmpty ->
                    DataGridTemplateColumn(
                        Header = column,
                        CellTemplate = FuncDataTemplate(
                            typeof<RowViewModel>,
                            Func<obj, INameScope, Control>(fun item _ ->
                                let row = item :?> RowViewModel
                                let textBlock = TextBlock(Text = row[i]) 
                                textBlock.Classes.Add("EnumValue")
                                textBlock :> Control
                            ),
                            false
                        ),
                        CellEditingTemplate = FuncDataTemplate(
                            typeof<RowViewModel>,
                            Func<obj, INameScope, Control>(fun item _ ->
                                let row = item :?> RowViewModel
                                if enumInfo.isFixed then
                                    let comboBox = ComboBox(ItemsSource = enumInfo.values)
                                    comboBox.Bind(
                                        ComboBox.SelectedItemProperty,
                                        Binding($"[{i}]")
                                    ) |> ignore
                                    comboBox :> Control
                                else
                                    let autoCompleteBox = AutoCompleteBox(ItemsSource = enumInfo.values)
                                    autoCompleteBox.Bind(
                                        AutoCompleteBox.TextProperty,
                                        Binding($"[{i}]")
                                    ) |> ignore
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

    member private self.UploadTableDataGrid_CurrentCellChanged(sender : obj, e : EventArgs) =
        sender
        :?> DataGrid
        |> displayDataGridCellInfo

    member private self.MoneyTableDataGrid_CurrentCellChanged(sender : obj, e : EventArgs) =
        ()

    member this.UploadTableDataGrid_KeyDown(sender : obj, e : KeyEventArgs) =
        let dataGrid = sender :?> DataGrid
        if not dataGrid.IsReadOnly then
            match e.Key with
            | Key.Enter | Key.F2 | Key.Insert when not windowViewModel.UploadTable.IsInEditMode ->
                if dataGrid.SelectedItem <> null && dataGrid.CurrentColumn <> null then
                    dataGrid.BeginEdit() |> ignore
                    e.Handled <- true
            | Key.Enter when e.KeyModifiers.HasFlag KeyModifiers.Shift ->
                e.Handled <- true
            | Key.Enter | Key.Tab | Key.Escape when windowViewModel.UploadTable.IsInEditMode ->
                if dataGrid.SelectedItem <> null && dataGrid.CurrentColumn <> null then
                    dataGrid.CommitEdit() |> ignore
                    dataGrid.Reselect()
                    e.Handled <- true
            | Key.Delete when not dataGrid.IsReadOnly ->
                e.Handled <- true
            | _ -> ()

    member private self.DeleteRowsButton_Click(sender : obj, event : RoutedEventArgs) =
        windowViewModel.UploadTable.DeleteSelected()


    (* Money Document *)
    member private self.NewMoneyDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        windowConfig.MoneyDocument.DocumentPath <- ""
        self.TryLoadMoneyDocument() |> ignore
        ()

    member private self.LoadMoneyDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        ()

    member private self.SaveMoneyDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        ()

    member private self.SaveAsMoneyDocumentButton_Click(sender : obj, event : RoutedEventArgs) =
        ()

    member private self.DeleteMoneyDocumentRowsButton_Click(sender : obj, event : RoutedEventArgs) =
        windowViewModel.MoneyTable.DeleteSelected()