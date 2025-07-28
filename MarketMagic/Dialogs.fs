module MarketMagic.Dialogs

open System
open Avalonia
open Avalonia.Controls
open MsBox.Avalonia
open MsBox.Avalonia.Enums

let showError (msg : string) (owner : Window) =
    MessageBoxManager
        .GetMessageBoxStandard(
            "Error",
            msg,
            ButtonEnum.Ok,
            Icon.Error
        )
        .ShowWindowDialogAsync(owner)

module Unit =

    let showError (msg : string) (owner : Window) = task {
        let! _ = owner |> showError msg
        ()
    }