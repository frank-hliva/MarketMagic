namespace MarketMagic

open System
open System.Reflection
open System.IO
open System.IO.Compression
open Lime

module Url =
    let withPort (port : int) (url : string) =            
        $"""{if url.EndsWith("/") then url[..url.Length - 2] else url}:{port}"""

module Directories =
    let homeDir = Environment.GetFolderPath Environment.SpecialFolder.UserProfile

module MarketMagicApp =

    let name = Assembly.GetExecutingAssembly().GetName().Name

    let execPath = Assembly.GetExecutingAssembly().Location

    let directory = execPath |> Path.GetDirectoryName

    let private checkDirectory dir =
        if not <| Directory.Exists dir then Directory.CreateDirectory dir |> ignore
        dir

    let private toSubDirectory (subDirectoryName : string) =
        Directories.homeDir /+ subDirectoryName /+ name |> checkDirectory

    let configDir = ".config" |> toSubDirectory
    let shareDir = ".local" /+ "share" |> toSubDirectory

    let toConfigPath path = configDir /+ path
    let toSharedPath path = shareDir /+ path

    let appConfig = toConfigPath "app.conf"

    let setupTarget = shareDir

    let unzipFile (destPath: string) (zipFilePath: string) =
        if not (Directory.Exists(destPath)) then
            Directory.CreateDirectory(destPath) |> ignore
        ZipFile.ExtractToDirectory(zipFilePath, destPath)

    let setup () =
        let setupFilePath = directory /+ "Setup.zip"
        if File.Exists setupFilePath then
            setupFilePath |> unzipFile setupTarget
            File.Delete setupFilePath