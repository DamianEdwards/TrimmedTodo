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

### App execution times, size, and memory use after publishing

- Results taken on Windows 11 x64
- All apps are published using "Release" configuration
- Only trimming modes that result in the app still working are shown
- Execution time measures time for the app to run (startup, execute, close). Web apps are configured to send a request to themselves on startup and then shut themselves down when being measured.

|                              Project |       PublishKind |       Mean |     Error |    StdDev |  App Size | App Memory |
|   ---------------------------------- |------------------ |-----------:|----------:|----------:|----------:|-----------:|
|                   HelloWorld.Console |     SelfContained |   35.71 ms |  34.12 ms |  22.57 ms |  69.45 MB |         NA |
|                   HelloWorld.Console |           Trimmed |   33.97 ms |  17.81 ms |  11.78 ms |  11.05 MB |         NA |
|                   HelloWorld.Console | TrimmedReadyToRun |   26.34 ms |  23.78 ms |  15.73 ms |  14.27 MB |         NA |
|                   HelloWorld.Console |               AOT |   10.17 ms |   6.20 ms |   4.10 ms |   0.97 MB |         NA |
|                                      |                   |            |           |           |           |            |
|              HelloWorld.HttpListener |     SelfContained |  117.83 ms |  55.49 ms |  36.70 ms |  69.46 MB |   32.03 MB |
|              HelloWorld.HttpListener |           Trimmed |  275.14 ms |  32.25 ms |  21.33 ms |  12.96 MB |   27.11 MB |
|              HelloWorld.HttpListener | TrimmedReadyToRun |   96.63 ms |  33.83 ms |  22.38 ms |  20.36 MB |   26.02 MB |
|              HelloWorld.HttpListener |               AOT |   55.04 ms |   8.69 ms |  12.36 ms |   8.72 MB |   14.59 MB |
|                                      |                   |            |           |           |           |            |
|                       HelloWorld.Web |     SelfContained |  233.89 ms | 102.21 ms |  67.61 ms |  94.23 MB |   50.56 MB |
|                       HelloWorld.Web |           Trimmed |  507.58 ms |  40.40 ms |  26.72 ms |  17.28 MB |   40.39 MB |
|                       HelloWorld.Web | TrimmedReadyToRun |  158.79 ms |  59.21 ms |  39.16 ms |  37.93 MB |   43.45 MB |
|                       HelloWorld.Web |               AOT |   72.97 ms |  43.89 ms |  29.03 ms |  21.86 MB |   36.15 MB |
|                                      |                   |            |           |           |           |            |
|              HelloWorld.Web.Stripped |     SelfContained |  173.00 ms |  74.39 ms |  49.21 ms |  94.23 MB |   40.77 MB |
|              HelloWorld.Web.Stripped |           Trimmed |  348.29 ms |  27.74 ms |  18.35 ms |  14.49 MB |   32.32 MB |
|              HelloWorld.Web.Stripped | TrimmedReadyToRun |  118.70 ms |  38.42 ms |  25.41 ms |  25.01 MB |   33.15 MB |
|              HelloWorld.Web.Stripped |               AOT |   51.85 ms |  29.12 ms |  19.26 ms |  12.22 MB |   20.92 MB |
|                                      |                   |            |           |           |           |            |
|    TrimmedTodo.Console.EfCore.Sqlite |     SelfContained |   591.4 ms |  94.40 ms |  62.44 ms |  75.78 MB |         NA |
|    TrimmedTodo.Console.EfCore.Sqlite |           Trimmed |   834.0 ms | 107.61 ms |  71.18 ms |  22.07 MB |         NA |
|    TrimmedTodo.Console.EfCore.Sqlite | TrimmedReadyToRun |   280.3 ms |  98.34 ms |  65.05 ms |  51.86 MB |         NA |
|                                      |                   |            |           |           |           |            |
| TrimmedTodo.MinimalApi.Dapper.Sqlite |     SelfContained |   385.4 ms | 177.86 ms | 117.64 ms | 101.19 MB |   68.15 MB |
| TrimmedTodo.MinimalApi.Dapper.Sqlite |           Trimmed |   935.4 ms |  81.02 ms |  53.59 ms |  32.05 MB |   60.37 MB |
| TrimmedTodo.MinimalApi.Dapper.Sqlite | TrimmedReadyToRun |   286.4 ms | 118.29 ms |  78.24 ms |  69.05 MB |   65.18 MB |
|                                      |                   |            |           |           |           |            |
| TrimmedTodo.MinimalApi.EfCore.Sqlite |     SelfContained |   760.9 ms | 217.75 ms | 144.03 ms | 105.14 MB |   83.41 MB |
| TrimmedTodo.MinimalApi.EfCore.Sqlite |           Trimmed | 1,308.9 ms | 103.56 ms |  68.50 ms |  36.28 MB |   75.46 MB |
| TrimmedTodo.MinimalApi.EfCore.Sqlite | TrimmedReadyToRun |   415.1 ms | 136.38 ms |  90.21 ms |  81.72 MB |   81.32 MB |
|                                      |                   |            |           |           |           |            |
|        TrimmedTodo.MinimalApi.Sqlite |     SelfContained |   370.1 ms | 160.85 ms | 106.39 ms | 101.00 MB |   66.36 MB |
|        TrimmedTodo.MinimalApi.Sqlite |           Trimmed |   829.5 ms |  61.36 ms |  40.59 ms |  31.82 MB |   58.36 MB |
|        TrimmedTodo.MinimalApi.Sqlite | TrimmedReadyToRun |   273.3 ms | 111.96 ms |  74.06 ms |  68.33 MB |   63.04 MB |
|                                      |                   |            |           |           |           |            |
|     TrimmedTodo.WebApi.EfCore.Sqlite |     SelfContained |   728.9 ms | 221.46 ms | 146.48 ms | 105.10 MB |   85.73 MB |
|     TrimmedTodo.WebApi.EfCore.Sqlite |           Trimmed | 1,303.2 ms | 157.14 ms | 103.94 ms |  39.48 MB |   77.83 MB |
|     TrimmedTodo.WebApi.EfCore.Sqlite | TrimmedReadyToRun |   423.3 ms | 138.01 ms |  91.28 ms |  86.26 MB |   84.92 MB |
