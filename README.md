# Trimmed TODO

## What is this?

An exploration of [.NET 7 trimming](https://docs.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) on a TODO app using ASP.NET Core and Entity Framework Core.

Read more about [trimming in .NET apps in the official docs](https://docs.microsoft.com/dotnet/core/deploying/trimming/trimming-options#trimming-framework-library-features).

### Requirements

This solution currently uses a daily .NET 7 SDK `main` build (see exact min-version required in the [`global.json`](global.json)). You can grab such a build from the [installer repo](https://github.com/dotnet/installer).

### Running

From the repo root, run one of the PowerShell scripts to publish and run the console app or API app.

```cmd
> .\Run-TodoConsole.ps1
Cleaning up previous run
Publishing TrimmedTodo.Console.EfCore.Sqlite: dotnet publish -c Release -r win-x64 --self-contained
  Determining projects to restore...
  Restored ~\TrimmedTodo\src\TrimmedTodo.Console.EfCore.Sqlite\TrimmedTodo.Console.EfCore.Sqlite.csproj (i
  n 194 ms).
C:\Program Files\dotnet\sdk\7.0.100-preview.7.22351.2\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.target
s(219,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [~\TrimmedTodo\src\TrimmedTodo.Console.EfCore.Sqlite\TrimmedTodo.Console.EfCore.Sqlite.csproj]
  TrimmedTodo.Console.EfCore.Sqlite -> ~\TrimmedTodo\src\TrimmedTodo.Console.EfCore.Sqlite\bin\Release\net7.0\win-x64\TrimmedTodo.Console.EfCore.Sqlite.dll
  Optimizing assemblies for size. This process might take a while.
  TrimmedTodo.Console.EfCore.Sqlite -> ~\TrimmedTodo\.artifacts\TrimmedTodo.Console.EfCore.Sqlite\

App executable size: 21.19 MB
App last access time: 07/06/2022 21:32:29

Running TrimmedTodo.Console.EfCore.Sqlite

Ensuring database exists and is up to date at connection string 'Data Source=todos.db'

There are currently no todos!

Added todo 1
Added todo 2
Added todo 3

Id Title
----------------------
1  Do the groceries
2  Give the dog a bath
2  Wash the car

Todo 'Wash the car' completed!

Id Title
----------------------
1  Do the groceries
2  Give the dog a bath

Deleted all 3 todos!
```
