namespace MarketMagic

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.ComponentModel
open System.Runtime.CompilerServices

type RowViewModel(cells: string[]) =
    inherit BasicViewModel()

    let mutable cellValues = cells

    member self.Item
        with get(i) = cellValues[i]
        and set(i) value =
            cellValues[i] <- value
            self.OnPropertyChanged($"Item[{i}]")