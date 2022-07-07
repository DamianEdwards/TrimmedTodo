# Trimmed TODO

## What is this?

An exploration of [.NET 7 trimming](https://docs.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) on a TODO app using ASP.NET Core and Entity Framework Core.

### Requirements

This solution currently uses a daily .NET 7 SDK `main` build (see exact min-version required in the [`global.json`](global.json)). You can grab such a build from https://github.com/dotnet/installer

### Running

From the repo root, run one of the PowerShell scripts to publish and run the console app or web app.

```cmd
> .\Run-TodoConsole.ps1
Cleaning up previous run
Publishing TodoConsole
Running TodoConsole

Ensuring database exists and is up to date at connection string 'Data Source=todos.db'

There are currently no todos!

Added todo 19
Added todo 20
Added todo 21

Id Title
----------------------
19 Do the groceries
20 Give the dog a bath
21 Wash the car

Todo 'Wash the car' completed!

Id Title
----------------------
19 Do the groceries
20 Give the dog a bath

Deleted all 3 todos!
```
