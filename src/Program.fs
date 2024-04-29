open Argu
open Fli
open Tomlyn


[<CLIMutable>]
type FSCommand = {
    Name: string
    Command: string
    Args: ResizeArray<string>
    Steps: ResizeArray<string>
}


[<CLIMutable>]
type FSConfig = {
    Commands: ResizeArray<FSCommand>
}


type CliCommand =
    | [<MainCommand; Unique>] Name of string
    | [<AltCommandLine("-q")>] Quiet

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name name -> $"Name of the command to run"
            | Quiet -> $"Don't print command's output"


let parser = ArgumentParser.Create<CliCommand>(programName="fsrun")

[<EntryPoint>]
let main argv =
    let results = parser.ParseCommandLine(argv, raiseOnUsage=false)
    
    if results.IsUsageRequested then
        printf $"{parser.PrintUsage()}\n"

    
    let name = results.GetResult(Name)
    let file = System.IO.File.ReadAllText("fsrun.toml")
    let config = Toml.ToModel<FSConfig>(file)
    printf $"{Seq.length config.Commands} commands found\n"
    printf $"Command to run: {name}\n"
    
    0
