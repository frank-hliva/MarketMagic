module Lime.Process

open System
open System.Text
open System.Collections.Generic

type Config =
    {
        FileName : string
        Arguments : string

        CreateNoWindow : bool
        Domain : string
        Environment : IDictionary<string, string>
        ErrorDialog : bool
        ErrorDialogParentHandle : nativeint
        LoadUserProfile : bool
        Password : System.Security.SecureString
        PasswordInClearText : string
        RedirectStandardError : bool
        RedirectStandardInput : bool
        RedirectStandardOutput : bool
        StandardErrorEncoding : Encoding
        StandardInputEncoding : Encoding
        StandardOutputEncoding : Encoding
        UseCredentialsForNetworkingOnly : bool
        UseShellExecute : bool
        UserName : string
        Verb : string
        WindowStyle : System.Diagnostics.ProcessWindowStyle
        WorkingDirectory : string
    }


module Config =
    let empty =
        {
            FileName = ""
            Arguments = ""
            CreateNoWindow = false
            Domain = ""
            Environment = Dictionary<string, string>() :> IDictionary<string, string>
            ErrorDialog = false
            ErrorDialogParentHandle = nativeint 0
            LoadUserProfile = false
            Password = null
            PasswordInClearText = null
            RedirectStandardError = false
            RedirectStandardInput = false
            RedirectStandardOutput = false
            StandardErrorEncoding = null
            StandardInputEncoding = null
            StandardOutputEncoding = null
            UseCredentialsForNetworkingOnly = false
            UseShellExecute = true
            UserName = ""
            Verb = ""
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal
            WorkingDirectory = ""
        }

    let inline defaultWith (encoding : Encoding) =
        { 
            empty with
                UseShellExecute = false
                CreateNoWindow = true
                RedirectStandardError = true
                RedirectStandardInput = true
                RedirectStandardOutput = true
                StandardErrorEncoding = encoding
                StandardInputEncoding = encoding
                StandardOutputEncoding = encoding
        }

    let inline withFileName fileName config = { config with FileName = fileName }
    let inline withArguments args config = { config with Arguments = args }
    let inline withCreateNoWindow createNoWindow config = { config with CreateNoWindow = createNoWindow }
    let inline withDomain domain config = { config with Domain = domain }
    let inline withEnvironment env config = { config with Environment = env }
    let inline withErrorDialog errorDialog config = { config with ErrorDialog = errorDialog }
    let inline withErrorDialogParentHandle handle config = { config with ErrorDialogParentHandle = handle }
    let inline withLoadUserProfile loadUserProfile config = { config with LoadUserProfile = loadUserProfile }
    let inline withPassword password config = { config with Password = password }
    let inline withPasswordInClearText passwordInClearText config = { config with PasswordInClearText = passwordInClearText }
    let inline withRedirectStandardError redirect config = { config with RedirectStandardError = redirect }
    let inline withRedirectStandardInput redirect config = { config with RedirectStandardInput = redirect }
    let inline withRedirectStandardOutput redirect config = { config with RedirectStandardOutput = redirect }
    let inline withStandardErrorEncoding encoding config = { config with StandardErrorEncoding = encoding }
    let inline withStandardInputEncoding encoding config = { config with StandardInputEncoding = encoding }
    let inline withStandardOutputEncoding encoding config = { config with StandardOutputEncoding = encoding }
    let inline withUseCredentialsForNetworkingOnly useCredentials config = { config with UseCredentialsForNetworkingOnly = useCredentials }
    let inline withUseShellExecute useShellExecute config = { config with UseShellExecute = useShellExecute }
    let inline withUserName userName config = { config with UserName = userName }
    let inline withVerb verb config = { config with Verb = verb }
    let inline withWindowStyle windowStyle config = { config with WindowStyle = windowStyle }
    let inline withWorkingDirectory workingDirectory config = { config with WorkingDirectory = workingDirectory }

    let inline toProcessStartInfo (config: Config) =
        let processStartInfo =
            System.Diagnostics.ProcessStartInfo(
                fileName = config.FileName, arguments = config.Arguments,
                CreateNoWindow = config.CreateNoWindow,
                Domain = config.Domain,
                ErrorDialog = config.ErrorDialog,
                ErrorDialogParentHandle = config.ErrorDialogParentHandle,
                LoadUserProfile = config.LoadUserProfile,
                Password = config.Password,
                PasswordInClearText = config.PasswordInClearText,
                RedirectStandardError = config.RedirectStandardError,
                RedirectStandardInput = config.RedirectStandardInput,
                RedirectStandardOutput = config.RedirectStandardOutput,
                StandardErrorEncoding = config.StandardErrorEncoding,
                StandardInputEncoding = config.StandardInputEncoding,
                StandardOutputEncoding = config.StandardOutputEncoding,
                UseCredentialsForNetworkingOnly = config.UseCredentialsForNetworkingOnly,
                UseShellExecute = config.UseShellExecute,
                UserName = config.UserName,
                Verb = config.Verb,
                WindowStyle = config.WindowStyle,
                WorkingDirectory = config.WorkingDirectory
            )
        for kvp in config.Environment do
            processStartInfo.Environment.Add(kvp.Key, kvp.Value)
        processStartInfo

let inline create config =
    new System.Diagnostics.Process(StartInfo = Config.toProcessStartInfo config)

module Native =
    let inline start (processStartInfo : System.Diagnostics.ProcessStartInfo) =
        processStartInfo |> System.Diagnostics.Process.Start

let inline start config =
    config
    |> Config.toProcessStartInfo
    |> Native.start

let inline start' (proc : System.Diagnostics.Process) =
    proc.Start(), proc