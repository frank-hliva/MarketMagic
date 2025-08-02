namespace MarketMagic

open System
open System.IO
open System.Runtime.CompilerServices
open System.Collections.Generic
open Avalonia
open Avalonia.Controls
open Avalonia.Data
open Avalonia.Markup.Xaml
open Avalonia.Threading


[<Extension>]
type DataGridExtensions() =

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