module MarketMagic.Dialogs

open System
open Avalonia
open Avalonia.Controls
open MsBox.Avalonia
open MsBox.Avalonia.Enums

let showError (text : string) (owner : Window) =
    MessageBoxManager
        .GetMessageBoxStandard(
            "Error",
            text,
            ButtonEnum.Ok,
            Icon.Error
        )
        .ShowWindowDialogAsync(owner)

