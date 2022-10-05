# Trimmed TODO

## What is this?

An exploration of [.NET 7 trimming](https://docs.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) on a various simple apps including a TODO app using ASP.NET Core and Entity Framework Core.

Read more about [trimming in .NET apps in the official docs](https://docs.microsoft.com/dotnet/core/deploying/trimming/trimming-options#trimming-framework-library-features).

### Requirements

This solution currently uses a daily .NET 7 SDK `main` build (see exact min-version required in the [`global.json`](global.json)). You can grab such a build from the [installer repo](https://github.com/dotnet/installer).

### Running

From the repo root, run one of the PowerShell scripts to publish and run the various apps.

```terminal
> .\Run-TodoConsole.ps1
Cleaning up previous run
Publishing TrimmedTodo.Console.EfCore.Sqlite: dotnet publish -c Release -r win10-x64 --self-contained
  Determining projects to restore...
  Restored ~\TrimmedTodo\src\TrimmedTodo.Console.EfCore.Sqlite\TrimmedTodo.Console.EfCore.Sqlite.csproj (i
  n 194 ms).
C:\Program Files\dotnet\sdk\7.0.100-preview.7.22351.2\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.target
s(219,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [~\TrimmedTodo\src\TrimmedTodo.Console.EfCore.Sqlite\TrimmedTodo.Console.EfCore.Sqlite.csproj]
  TrimmedTodo.Console.EfCore.Sqlite -> ~\TrimmedTodo\src\TrimmedTodo.Console.EfCore.Sqlite\bin\Release\net7.0\win-x64\TrimmedTodo.Console.EfCore.Sqlite.dll
  Optimizing assemblies for size. This process might take a while.
  TrimmedTodo.Console.EfCore.Sqlite -> ~\TrimmedTodo\.artifacts\TrimmedTodo.Console.EfCore.Sqlite\

App was published as a single 21.78 MB file (TrimmedTodo.Console.EfCore.Sqlite.exe)

App last access time: 08/30/2022 17:30:13

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
>
```

### App sizes after publishing

- Results taken on Windows 11 x64
- All apps are published using "Release" configuration and are self-contained
- Only trimming modes that result in the app still working are shown
- Execution time measures time for the app to run (startup, execute, close). Web apps are configured to send a request to themselves on startup and then shut themselves down when being measured.

App type | Trim mode | Size
---------|-----------|-----:
[Console - Hello World](/src/HelloWorld.Console/) | full | 11.04 MB
[Console - Hello World](/src/HelloWorld.Console/) | AOT + full | 3.32 MB
[Console - Hello World](/src/HelloWorld.Console/) | AOT + full + tweaks | 2.18 MB
[Console - Hello World](/src/HelloWorld.Console/) | AOT + full + tweaks + hacks | **0.96 MB**
[Console - Todo EF Core & Sqlite](/src/TrimmedTodo.Console.EfCore.Sqlite/) | partial | 22.39 MB
[Console - Todo EF Core & Sqlite](/src/TrimmedTodo.Console.EfCore.Sqlite/) | full | 20.52 MB
[Console - Todo API client](/src/TrimmedTodo.Console.ApiClient/) | AOT + full | 9.12 MB
[Console - Todo API client](/src/TrimmedTodo.Console.ApiClient/) | AOT + full + tweaks | 8.92 MB
[Console - Todo API client](/src/TrimmedTodo.Console.ApiClient/) | AOT + full + tweaks + hacks | 8.28 MB
[Web - Hello World](/src/HelloWorld.Web/) | partial | 28.93 MB
[Web - Hello World](/src/HelloWorld.Web/) | full | 17.28 MB
[Web - Hello World](/src/HelloWorld.Web/) | AOT + full | 22.07 MB
[Web - Hello World](/src/HelloWorld.Web/) | AOT + full + tweaks | 21.84 MB
[Web - Hello World](/src/HelloWorld.Web/) | AOT + full + tweaks + hacks | 20.43 MB
[Web - Todo Minimal API EF Core & Sqlite](/src/TrimmedTodo.MinimalApi.EfCore.Sqlite/) | partial | 39.80 MB
[Web - Todo Minimal API EF Core & Sqlite](/src/TrimmedTodo.MinimalApi.EfCore.Sqlite/) | partial + tweaks | 36.56 MB
[Web - Todo Minimal API Dapper & Sqlite](/src/TrimmedTodo.MinimalApi.Dapper.Sqlite/) | partial | 35.43 MB
[Web - Todo Minimal API Dapper & Sqlite](/src/TrimmedTodo.MinimalApi.Dapper.Sqlite/) | partial + tweaks | 32.77 MB
[Web - Todo MVC Web API EF Core & Sqlite](/src/TrimmedTodo.WebApi.EfCore.Sqlite/) | partial + tweaks | 39.75 MB

### App execution times after publishing

- Results taken on Windows 11 x64
- All apps are published using "Release" configuration
- Only trimming modes that result in the app still working are shown
- Execution time measures time for the app to run (startup, execute, close). Web apps are configured to send a request to themselves on startup and then shut themselves down when being measured.

|                          ProjectName |          Scenario |        Mean |     Error |    StdDev |
|------------------------------------- |------------------ |------------:|----------:|----------:|
|                   HelloWorld.Console |           Trimmed |    38.76 ms | 21.496 ms | 14.218 ms |
|                   HelloWorld.Console | TrimmedReadyToRun |    27.52 ms | 24.214 ms | 16.016 ms |
|                   HelloWorld.Console |               AOT |    12.90 ms |  6.830 ms |  4.518 ms |
|                                      |                   |             |           |           |
|                       HelloWorld.Web |           Trimmed |   540.81 ms | 43.958 ms | 29.076 ms |
|                       HelloWorld.Web | TrimmedReadyToRun |   173.49 ms | 63.661 ms | 42.108 ms |
|                       HelloWorld.Web |               AOT |    80.17 ms | 43.839 ms | 28.997 ms |
|                                      |                   |             |           |           |
|    TrimmedTodo.Console.EfCore.Sqlite |           Trimmed |    898.9 ms |  58.72 ms |  38.84 ms |
|    TrimmedTodo.Console.EfCore.Sqlite | TrimmedReadyToRun |    314.3 ms | 129.64 ms |  85.75 ms |
|                                      |                   |             |           |           |
| TrimmedTodo.MinimalApi.Dapper.Sqlite |           Trimmed |   952.64 ms |  64.99 ms |  42.99 ms |
| TrimmedTodo.MinimalApi.Dapper.Sqlite | TrimmedReadyToRun |   295.68 ms | 116.17 ms |  76.84 ms |
|                                      |                   |             |           |           |
| TrimmedTodo.MinimalApi.EfCore.Sqlite |           Trimmed | 1,400.27 ms |  91.75 ms |  60.68 ms |
| TrimmedTodo.MinimalApi.EfCore.Sqlite | TrimmedReadyToRun |   460.99 ms | 142.61 ms |  94.33 ms |
|                                      |                   |             |           |           |
|     TrimmedTodo.WebApi.EfCore.Sqlite |           Trimmed | 1,416.72 ms | 148.68 ms |  98.34 ms |
|     TrimmedTodo.WebApi.EfCore.Sqlite | TrimmedReadyToRun |   510.78 ms | 344.67 ms | 227.98 ms |
