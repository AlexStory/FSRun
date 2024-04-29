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

## Flags

Here are some of the flags that you can pass to fsrun.

### --list

Prints the help text for the tool.

### --logs \<filepath>

Will write all output logs to the specified file in append mode. Will create the file if it does not exist.

### --file, -f \<filename>

Name of the file to run for config instead of `fsrun.toml`. Will look upwards recursively through directories attempting to find the file. Can be useful if you want to have different config files for different environments in the same project.

### --quiet

Silence output from run commands in the terminal