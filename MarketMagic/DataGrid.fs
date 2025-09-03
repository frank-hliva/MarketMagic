namespace MarketMagic

open System
open System.IO
open System.Runtime.CompilerServices
open System.Collections.ObjectModel
open Avalonia
open Avalonia.Controls
open Avalonia.Data
open Avalonia.Markup.Xaml
open Avalonia.Threading
open Avalonia.Controls.Templates
open MarketMagic

[<Extension>]
type DataGridExtensions() =

    static let setupColumns (tableViewModel : TableViewModel) (dataGrid : DataGrid) =
        dataGrid.Columns.Clear()
        DataGridCheckBoxColumn(
            Header = "#",
            Binding = Binding("IsMarked"),
            Width = DataGridLength(40.0, DataGridLengthUnitType.Pixel)
        ) |> dataGrid.Columns.Add

        let enums =
            tableViewModel.DataTable
            |> Option.map _.enums
            |> Option.defaultValue Map.empty

        for i in 0 .. tableViewModel.Columns.Count - 1 do
            let column = tableViewModel.Columns[i]
            dataGrid.Columns.Add(
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

    [<Extension>]
    static member GetDataGridCellInfo(self : DataGrid, isInEditMode : bool) =
        let rowIndex = self.SelectedIndex
        let columnIndex = 
            match self.CurrentColumn with
            | null -> -1
            | col -> col.DisplayIndex
        $"Row: {rowIndex + 1}, Column: {columnIndex + 1}",
        if self.IsFocused then
            if isInEditMode
            then "Press the [⏎] or [Tab] or [Esc] key to exit edit mode"
            else "Press the [⏎] or [F2] or [Ins] to edit"
        else ""

    [<Extension>]
    static member Reselect (self : DataGrid) =
        self.Focus() |> ignore
        self.UpdateLayout()
        let currentRow = self.SelectedIndex
        let currentCol = self.CurrentColumn
        self.SelectedIndex <- -1
        self.SelectedIndex <- currentRow
        self.CurrentColumn <- currentCol

    [<Extension>]
    static member SetupColumns (self : DataGrid, tableViewModel : TableViewModel) =
        self |> setupColumns tableViewModel
