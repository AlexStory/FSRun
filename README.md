# FSRun

FSRun is a language agnostic task runner. I often miss the ease of use of being able to put repetitive tasks in the scripts section of package.json, so I've tried to create something similar that can be used in any language.

## Getting started

The easiest way to install right now is to add it as a .net tool `dotnet tool install --global fsrun`

You can also clone the repo from source, then you can do `dotnet run publish[-platform]`. Then can copy the executable to your path.

To use it make a config file. (fsrun.toml is the default, but you can change the name)

here's an example with one command that just echos hello world.

fsrun.toml
```toml
[commands]
greet = "echo hello, world!"
```
then you could run `fsrun greet` and it would write `hello, world!` to your console.

You can have multiple commands in the block, and you can break out commands to a separate table if you need to be more precise with them.

```toml
[commands]
clean = "dotnet clean"
build = "dotnet build"

[commands.publish]
command = "dotnet"
args = ["publish", "-r", "linux-x64", "--self-contained", "true"]
```

You can also use multiple staged commands by referencing other commands in a list.

```toml
[commands]
clean = "dotnet clean"
build = "dotnet build"
rebuild = ["clean", "build"]
```

You can also set environment variables that you want added to all commands.

```toml
[environment]
ENV = "development"

[commands]
script = "echo $ENV"
```
```
$ fsrun script
development
```

You can also set environment vars for an individual command.
```toml
[commands.echo]
command = "echo"
args = ["$VAR"]
environment = { VAR = "my variable" }
```

The working directory to run a command from can be set much the same way. It can be set at the root level, or on a detailed command. With any setting, the value on a specific commad will take percedence over a global setting.

```toml
working_directory = "./src"

[commands.list-packages]
command = "ls"
working_directory = "./node_modules"
```

## Toml Settings

### Root Settings

- `working_directory` - directory to run commands from. a string.

- `[environment]` - a list of env variables to be loaded for all commands. `key = "value"`

- `[commands]` - the list of commands to run. Can be in simple or detailed form. Simple follows the `name = "cmd args"` format. Detailed commands are described below.

### Command Level Settings

More detailed commands are set in tables with the format `[commands.name]` where `name` is the name of the command.

- `command` - the command that you want to run. a string.
- `args` - the list of arguments to pass to the command. a list of strings.
- `[environment]` - key value pairs of values to be added to the environment for the command. a toml table.
- `working_directory` - the directory from which to run the command. a string.


## Flags

Here are some of the flags that you can pass to fsrun.

### --help

Prints the help text for the tool.

### --list

Lists all the commands that can be run for the project

### --init

Initializes a basic config file

### --logs \<filepath>

Will write all output logs to the specified file in append mode. Will create the file if it does not exist.

### --file, -f \<filename>

Name of the file to run for config instead of `fsrun.toml`. Will look upwards recursively through directories attempting to find the file. Can be useful if you want to have different config files for different environments in the same project.

### --quiet

Silence output from run commands in the terminal