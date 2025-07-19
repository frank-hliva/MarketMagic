namespace MarketMagic

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.ComponentModel
open System.Runtime.CompilerServices

[<AbstractClass>]
type BasicViewModel() as vm =
    let propertyChangedEvent = new Event<_, _>()

    let iNotifyPropertyChanged = vm :> INotifyPropertyChanged

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member vm.PropertyChanged = propertyChangedEvent.Publish

    member vm.INotifyPropertyChanged = iNotifyPropertyChanged

    member vm.PropertyChanged = iNotifyPropertyChanged.PropertyChanged

    abstract OnPropertyChanged : string -> unit
    default vm.OnPropertyChanged propertyName = 
        propertyChangedEvent.Trigger(
            vm,
            PropertyChangedEventArgs(propertyName)
        )

    abstract OnPropertiesChanged : string seq -> unit
    default vm.OnPropertiesChanged propertyNames = 
        propertyNames |> Seq.iter vm.OnPropertyChanged