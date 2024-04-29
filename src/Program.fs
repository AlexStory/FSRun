open Argu
open Fli
open FSRun





type CliCommand =
    | [<MainCommand; Unique>] Name of string
    | [<AltCommandLine("-q")>] Quiet
    | List

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name name -> $"Name of the command to run"
            | Quiet -> $"Don't print command's output"
            | List -> $"List all commands"


type Action =
    | PrintUsage
    | ListCommands
    | RunCommand


let getAction (results: ParseResults<CliCommand>)  =
    if results.IsUsageRequested then
        PrintUsage
    else if results.Contains List then
        ListCommands
    else if results.Contains Name then
        RunCommand
    else
        PrintUsage


let rec runCommand (name: string) (config: Config.FSConfig) (results: ParseResults<CliCommand>) =
    match List.tryFind (fun (cmd: Config.FSCommand) -> cmd.Name = name) config.Commands with
    | Some cmd ->
        match cmd.Action with
        | Config.CommandAction.Steps steps ->
            steps
            |> List.map (fun step -> runCommand step config results)
            |> List.fold (fun acc code -> if acc = 0 then code else acc) 0
        | Config.CommandAction.Command (cmd, args) ->
            let result =
                cli {
                    Exec cmd
                    Arguments args
                    EnvironmentVariables (config.Environment |> Map.toList)
                }
                |> Command.execute
            if not (results.Contains Quiet) then
                result |> Output.printText
            result |> Output.toExitCode
    | None ->
        printf $"Command '{name}' not found\n"
        1


let performAction action (config: Config.FSConfig) (parser: ArgumentParser<CliCommand>) (results: ParseResults<CliCommand>) =
    match action with
    | PrintUsage -> 
        printf $"{parser.PrintUsage()}\n"
        0
    | ListCommands -> 
        for cmd in config.Commands do
            printf $"- {cmd.Name}\n"
        0
    | RunCommand ->
        let name = results.GetResult(Name, "default")
        runCommand name config results


let parser = ArgumentParser.Create<CliCommand>(programName="fsrun")


[<EntryPoint>]
let main argv =
    let results = parser.ParseCommandLine(argv, raiseOnUsage=false)
    let config = Config.fromFile "fsrun.toml"
    let action = getAction results
    performAction action config parser results
