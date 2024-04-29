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
}


type FSConfig = {
    Environment: Map<string, string>
    Commands: FSCommand list
}


[<CLIMutable>]
type FSCommandInterop = {
    Name: string
    Command: string
    Args: ResizeArray<string>
    Steps: ResizeArray<string>
}


type CommandValue =
    | SimpleCommand of string
    | StepsCommand of string list
    | DetailedCommand of FSCommandInterop


[<CLIMutable>]
type FSConfigInterop = {
    Environment: Dictionary<string, string>
    Commands: Dictionary<string, CommandValue>
}


let parseSimpleCommand (cmdLine: string) name =
    let regex = new Regex("""[ ](?=(?:[^""]*""[^""]*"")*[^""]*$)""")
    let parts = regex.Split(cmdLine)
    { Name = name; Action = Command (parts.[0], (parts.[1..] |> Array.toList)) }


let rec findFile filePath =
    if File.Exists(filePath) then
        Some filePath
    else
        let parentDirectory = Directory.GetParent(filePath)
        if parentDirectory <> null then
            let grandParentDirectory = parentDirectory.Parent
            if grandParentDirectory <> null then
                findFile (Path.Combine(grandParentDirectory.FullName, Path.GetFileName(filePath)))
            else
                None
        else
            None


let fromFile filePath : FSConfig =
    let file =
        findFile filePath
        |> Option.defaultWith(fun() ->
            printfn $"File not found: {filePath}"
            System.Environment.Exit 1
            failwith "unreachable"
        )
    let file = File.ReadAllText file
    let configInterop = Toml.ToModel(file)
    let tomlEnv: IDictionary<string, obj> =  if configInterop.ContainsKey("environment") then configInterop["environment"] :?> IDictionary<string, obj> else new Dictionary<string, obj>()
    let tomlCommands = if configInterop.ContainsKey("commands") then configInterop["commands"] :?> Model.TomlTable else new Model.TomlTable()
    let env = tomlEnv |> Seq.map (fun kvp -> kvp.Key, kvp.Value |> string) |> Map.ofSeq
    let commands =
        tomlCommands |> Seq.map (fun kvp -> 
            let name = kvp.Key
            let value = kvp.Value
            let commandValue = 
                match value with
                | :? string -> SimpleCommand ((string) value)
                | :? Model.TomlArray as arr -> StepsCommand (arr |> Seq.cast<string> |> List.ofSeq)
                | :? Model.TomlTable as tbl -> 
                    let cmd = if tbl.ContainsKey("command") then tbl["command"] :?> string else ""
                    let args = if tbl.ContainsKey("args") then tbl["args"] :?> IList<obj> |> Seq.cast<string> |> List.ofSeq else []
                    let steps = if tbl.ContainsKey("steps") then tbl["steps"] :?> IList<obj> |> Seq.cast<string> |> List.ofSeq else []
                    DetailedCommand { Name = name; Command = cmd; Args = args |> ResizeArray; Steps = steps |> ResizeArray }
                | _ -> failwith "Invalid command value"
            
            match commandValue with
            | SimpleCommand cmd -> parseSimpleCommand cmd name
            | StepsCommand steps -> { Name = name; Action = Steps steps }
            | DetailedCommand cmd -> { Name = name; Action = Command (cmd.Command, cmd.Args |> List.ofSeq) }
            
        ) |> List.ofSeq

    { Environment = env; Commands = commands }