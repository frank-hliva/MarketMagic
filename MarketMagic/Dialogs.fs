module MarketMagic.Dialogs

open System
open System.Runtime.InteropServices
open Avalonia
open Avalonia.Controls
open MsBox.Avalonia
open MsBox.Avalonia.Enums

module Native =

    type private MessageBoxDelegate = delegate of nativeint * string * string * uint32 -> int

    module Btn =
        let OK = 0u
        let CANCEL = 1u
        let ABORT = 2u
        let RETRY = 3u
        let IGNORE = 5u
        let YES = 6u
        let NO = 7u
        let TRYAGAIN = 10u
        let CONTINUE = 11u

    module Icon =
        let ERROR = 0x10u
        let QUESTION = 0x20u
        let WARNING = 0x30u
        let INFORMATION = 0x40u

    let show (title : string) (options : uint32) (msg : string) =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            let messageBox (title : string) (options : uint32) (text : string) =
                let user32 = NativeLibrary.Load("user32.dll")
                let proc = NativeLibrary.GetExport(user32, "MessageBoxA")
                let delegate' = Marshal.GetDelegateForFunctionPointer<MessageBoxDelegate>(proc)
                delegate'.Invoke(IntPtr.Zero, text, title, options) |> ignore
                NativeLibrary.Free(user32)
            msg |> messageBox title options
        else
            printfn $"{title}: {msg}"

    let showError = show "Error" (Btn.OK ||| Icon.ERROR)

let private toErrorBox (msg : string) =
    MessageBoxManager.GetMessageBoxStandard(
        "Error",
        msg,
        ButtonEnum.Ok,
        Icon.Error
    )

let showError (msg : string) (owner : Window) =
    msg
    |> toErrorBox
    |> _.ShowWindowDialogAsync(owner)

let showErrorU msg = showError msg >> Lime.Task.toUnitTask

module Pick =

    open Avalonia.Platform.Storage

    let filesToOpen (owner : TopLevel) (opts: {| title : string; allowMultiple : bool |}) (fileTypeFilter : FilePickerFileType list) = async {            
        match! owner.StorageProvider.OpenFilePickerAsync(
                FilePickerOpenOptions(
                    Title = opts.title,
                    AllowMultiple = opts.allowMultiple,
                    FileTypeFilter = fileTypeFilter
                )
            ) |> Async.AwaitTask with
        | null -> return []
        | files -> return List.ofSeq files
    }

    let filesToSave (owner : TopLevel) (opts: {| title : string |}) (fileTypeFilter : FilePickerFileType list) = async {            
        match! owner.StorageProvider.SaveFilePickerAsync(
                FilePickerSaveOptions(
                    Title = opts.title,
                    FileTypeChoices = fileTypeFilter
                )
            ) |> Async.AwaitTask with
        | null -> return []
        | file -> return [file]
    }