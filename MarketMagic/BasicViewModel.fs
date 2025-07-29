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
type BasicViewModel() as self =
    let propertyChangedEvent = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    let iNotifyPropertyChanged = self :> INotifyPropertyChanged

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member self.PropertyChanged = propertyChangedEvent.Publish

    member self.INotifyPropertyChanged = iNotifyPropertyChanged

    member self.PropertyChanged = iNotifyPropertyChanged.PropertyChanged

    abstract OnPropertyChanged : string -> unit
    default self.OnPropertyChanged propertyName = 
        propertyChangedEvent.Trigger(
            self,
            PropertyChangedEventArgs(propertyName)
        )

    abstract OnPropertiesChanged : string seq -> unit
    default self.OnPropertiesChanged propertyNames = 
        propertyNames |> Seq.iter self.OnPropertyChanged