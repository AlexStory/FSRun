module FSRun.Config

open System.Collections.Generic
open System.IO
open Tomlyn

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


[<CLIMutable>]
type FSConfigInterop = {
    Environment: Dictionary<string, string>
    Commands: ResizeArray<FSCommandInterop>
}

let fromFile filePath : FSConfig =
    let file = File.ReadAllText(filePath)
    let configInterop = Toml.ToModel<FSConfigInterop>(file)
    let environment = configInterop.Environment |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Map.ofSeq
    let commands = 
        configInterop.Commands |> Seq.map (fun cmd -> 
            let action = 
                if not (isNull cmd.Command) then
                    Command (cmd.Command, cmd.Args |> List.ofSeq)
                else
                    Steps (cmd.Steps |> List.ofSeq)
            { Name = cmd.Name; Action = action }
        )
        |> List.ofSeq
    { Environment = environment; Commands = commands }