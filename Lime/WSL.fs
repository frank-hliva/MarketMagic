module Lime.WSL

open System
open System.Text
open Lime
open Process
open System.Diagnostics

module Process =

    let startAndRead (command : string) (args : string) (config : Process.Config) : string =
        {
            config with
                FileName = command
                Arguments = args
        }
        |> Process.start
        |> function
            | null -> failwithf "Failed to start process: %s %s" command args
            | proc' ->
                use proc = proc'
                let output = proc.StandardOutput.ReadToEnd()
                proc.WaitForExit()
                output

module private ListCells =

    let ofString : string -> _ = _.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)

    let parseDistributionName (cells : string[]) =
        if cells.Length > 2
        then cells.[..cells.Length - (2 + 1)]
        else cells
        |> fun x -> String.Join(" ", x)

    let parseWSLVersion (cells : string[]) =
        if cells.Length > 2 then
            match cells |> Array.last |> Int32.TryParse with
            | true, version -> version
            | false, _ -> -1
        else -1

type DistributionInfo =
    {
        Name: string
        IsDefault: bool
        WSLVersion : int
    }

let private parseNonEmptyLines (input : string) =
    input
    |> _.ToLines() 
    |> Seq.map _.Trim()
    |> Seq.filter ((<>) "")

let getDistributions () =
    Process.Config.defaultWith Encoding.Unicode
    |> Process.startAndRead "wsl" "--list --verbose"
    |> parseNonEmptyLines
    |> Seq.skip 1
    |> Seq.map (fun (line : string) ->
        let isDefault = line.StartsWith("*")
        let line =
            if isDefault
            then line.TrimStart([|' '; '*'|])
            else line
        let cells = line |> ListCells.ofString
        {
            Name = cells |> ListCells.parseDistributionName
            IsDefault = isDefault
            WSLVersion = cells |> ListCells.parseWSLVersion
        }
    )

let getDefaultDistribution () =
    getDistributions() |> Seq.find _.IsDefault

let getDefaultShellProcess (distro : string) =
    Process.Config.defaultWith Encoding.ASCII
    |> Process.startAndRead "wsl" $"-d {distro} echo $SHELL"
    |> parseNonEmptyLines
    |> Seq.last

type EnvironmentConfig =
    {
        Distribution : string
        CommandPath : string
        DefaultShell : string
    }

type Env =
    {
        Distribution : string option
        CommandPath : string option
        DefaultShell : string option
    }

type IEnvironment =
    abstract Distribution : string option with get
    abstract CommandPath : string option with get
    abstract DefaultShell : string option with get

and Environment(env : Env) =
    interface IEnvironment with
        override this.Distribution with get() = env.Distribution
        override this.CommandPath with get() = env.CommandPath
        override this.DefaultShell with get() = env.DefaultShell
    new(envConfig : EnvironmentConfig) =
        Environment({
            Distribution = Some envConfig.Distribution
            CommandPath = Some envConfig.CommandPath
            DefaultShell = Some <| getDefaultShellProcess envConfig.Distribution
        })
    static member empty() =
        Environment({ Distribution = None; CommandPath = None; DefaultShell = None  })

type EnvironmentConfigAdapter(envConfig : EnvironmentConfig) =
    let defaultShell = getDefaultShellProcess envConfig.Distribution
    interface IEnvironment with
        member this.Distribution with get() = Some envConfig.Distribution
        member this.CommandPath with get() = Some envConfig.CommandPath
        member this.DefaultShell = Some defaultShell

module private Arguments =
    let internal ofCommandByEnv (command : string) (env : IEnvironment) =
        match env.CommandPath with
        | Some commandPath -> $"cd {commandPath};{command}"
        | _ -> command
        |> fun command ->
            match env.Distribution with
            | Some distro -> $"-d {distro} "
            | _ -> ""
            |> fun args -> 
                match env.DefaultShell with
                | Some defaultShell -> $"{args} -- {defaultShell} -l -c \"{command}\""
                | _ -> args + command