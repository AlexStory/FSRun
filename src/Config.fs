module FSRun.Config

open System.Collections.Generic
open System.IO
open Tomlyn
open System.Text.RegularExpressions


type CommandAction =
    | Steps of string list
    | Command of cmd: string * args: string list


type FSCommand = {
    Name: string
    Action: CommandAction
    Environment: Map<string, string>
    WorkingDirectory: string option
}


type FSConfig = {
    Environment: Map<string, string>
    Commands: FSCommand list
    WorkingDirectory: string
}


type CommandValue =
    | SimpleCommand of string
    | StepsCommand of string list
    | DetailedCommand of FSCommand


let parseSimpleCommand (cmdLine: string) name =
    let regex = new Regex("""[ ](?=(?:[^""]*""[^""]*"")*[^""]*$)""")
    let parts = regex.Split(cmdLine)

    {
        Name = name
        Action = Command(parts.[0], (parts.[1..] |> Array.toList))
        Environment = Map.empty
        WorkingDirectory = None
    }


let rec findFile filePath =
    match File.Exists(filePath) with
    | true -> Some filePath, filePath |> Path.GetDirectoryName
    | false ->
        let parentDirectory = Directory.GetParent(filePath)

        let grandParentDirectory =
            if parentDirectory <> null then
                parentDirectory.Parent
            else
                null

        if grandParentDirectory <> null then
            findFile (Path.Combine(grandParentDirectory.FullName, Path.GetFileName(filePath)))
        else
            None, ""


let parseEnvironment (table: Model.TomlTable) =
    let tomlEnv =
        if table.ContainsKey("environment") then
            table["environment"] :?> IDictionary<string, obj>
        else
            new Dictionary<string, obj>()

    tomlEnv |> Seq.map (fun kvp -> kvp.Key, kvp.Value |> string) |> Map.ofSeq


let parseDirectory (table: Model.TomlTable) =
    if table.ContainsKey("working_directory") then
        table["working_directory"] :?> string |> Some
    else
        None


let parseCommands (table: Model.TomlTable) =
    let tomlCommands =
        if table.ContainsKey("commands") then
            table["commands"] :?> Model.TomlTable
        else
            new Model.TomlTable()



    tomlCommands
    |> Seq.map (fun kvp ->
        let name = kvp.Key
        let value = kvp.Value

        let commandValue =
            match value with
            | :? string as s -> SimpleCommand s
            | :? Model.TomlArray as arr -> StepsCommand(arr |> Seq.cast<string> |> List.ofSeq)
            | :? Model.TomlTable as tbl ->
                let cmd =
                    if tbl.ContainsKey("command") then
                        tbl["command"] :?> string
                    else
                        ""

                let args =
                    if tbl.ContainsKey("args") then
                        tbl["args"] :?> IList<obj> |> Seq.cast<string> |> List.ofSeq
                    else
                        []

                let steps =
                    if tbl.ContainsKey("steps") then
                        tbl["steps"] :?> IList<obj> |> Seq.cast<string> |> List.ofSeq
                    else
                        []

                let cmdEnv = parseEnvironment tbl
                let cmdDirectory = parseDirectory tbl

                if cmd = "" then
                    DetailedCommand {
                        Name = name
                        Action = Steps steps
                        Environment = cmdEnv
                        WorkingDirectory = cmdDirectory
                    }
                else
                    DetailedCommand {
                        Name = name
                        Action = Command(cmd, args)
                        Environment = cmdEnv
                        WorkingDirectory = cmdDirectory
                    }
            | _ -> failwith "Invalid command value"

        match commandValue with
        | SimpleCommand cmd -> parseSimpleCommand cmd name
        | StepsCommand steps -> {
            Name = name
            Action = Steps steps
            Environment = Map.empty
            WorkingDirectory = None
          }
        | DetailedCommand cmd -> cmd

    )
    |> List.ofSeq


let fromFile filePath : FSConfig =
    let file, directory = findFile filePath

    let file =
        file
        |> Option.defaultWith (fun () ->
            printfn $"File not found: {filePath}"
            System.Environment.Exit 1
            failwith "unreachable")

    let file = File.ReadAllText file
    let configInterop = Toml.ToModel(file)
    let env = parseEnvironment configInterop
    let directory = parseDirectory configInterop |> Option.defaultValue directory
    let commands = parseCommands configInterop


    {
        Environment = env
        Commands = commands
        WorkingDirectory = directory
    }
