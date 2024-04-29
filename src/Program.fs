open Argu
open Fli
open FSRun
open System.IO


type CliCommand =
    | [<MainCommand; Unique>] Name of string
    | [<AltCommandLine("-q")>] Quiet
    | List
    | Logs of path: string
    | [<AltCommandLine("-f")>] File of path: string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name name -> $"Name of the command to run"
            | Quiet -> $"Don't print command's output"
            | List -> $"List all commands"
            | Logs path -> $"Log output to a file at the given path"
            | File path -> $"Path to the configuration file (default: fsrun.toml)"


// The action to take based on the command line arguments
type Action =
    | PrintUsage
    | ListCommands
    | RunCommand


type Settings = {
    Quiet: bool
    LogPath: string option
    Command: string option
    FileName: string
}

let parseSettings (results: ParseResults<CliCommand>) =
    let quiet = results.Contains Quiet
    let logPath = results.TryGetResult(Logs)
    let command = results.TryGetResult(Name)
    let fileName = results.GetResult(File, "fsrun.toml")
    { Quiet = quiet; LogPath = logPath; Command = command; FileName = fileName}


let getAction (results: ParseResults<CliCommand>)  =
    if results.IsUsageRequested then
        PrintUsage
    else if results.Contains List then
        ListCommands
    else if results.Contains Name then
        RunCommand
    else
        PrintUsage

let writeLog filePath text =
    if not (File.Exists(filePath)) then
        File.WriteAllText(filePath, text)
    else
        File.AppendAllText(filePath, text)

    File.AppendAllText(filePath, "\n")


let rec runCommand (name: string) (config: Config.FSConfig) settings =
    match List.tryFind (fun (cmd: Config.FSCommand) -> cmd.Name = name) config.Commands with
    | Some cmd ->
        match cmd.Action with
        | Config.CommandAction.Steps steps ->
            steps
            |> List.map (fun step -> runCommand step config settings)
            |> List.fold (fun acc code -> if acc = 0 then code else acc) 0
        | Config.CommandAction.Command (cmd, args) ->
            printfn $"""Running command: {cmd} {String.concat " " args}"""
            let result =
                cli {
                    Exec cmd
                    Arguments args
                    EnvironmentVariables (config.Environment |> Map.toList)
                }
                |> Command.execute
            if not (settings.Quiet) then
                result |> Output.printText
            if settings.LogPath.IsSome then
                writeLog settings.LogPath.Value result.Text.Value
            result |> Output.toExitCode
    | None ->
        printf $"Command '{name}' not found\n"
        1


let performAction action (config: Config.FSConfig) (parser: ArgumentParser<CliCommand>) settings =
    match action with
    | PrintUsage -> 
        printf $"{parser.PrintUsage()}\n"
        0
    | ListCommands -> 
        for cmd in config.Commands do
            printf $"- {cmd.Name}\n"
        0
    | RunCommand ->
        runCommand settings.Command.Value config settings


let parser = ArgumentParser.Create<CliCommand>(programName="fsrun")


[<EntryPoint>]
let main argv =
    let results = parser.ParseCommandLine(argv, raiseOnUsage=false)
    let settings = parseSettings results
    let config = Config.fromFile settings.FileName
    let action = getAction results
    performAction action config parser settings
