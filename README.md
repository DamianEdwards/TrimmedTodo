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

|                              Project |                 PublishKind |  Execution |  App Size | App Memory |
|   ---------------------------------- |---------------------------- |-----------:|----------:|-----------:|
|                   HelloWorld.Console |               SelfContained |   35.71 ms |  69.45 MB |         NA |
|                   HelloWorld.Console |                     Trimmed |   33.97 ms |  11.05 MB |         NA |
|                   HelloWorld.Console |           TrimmedCompressed |   43.46 ms |   9.82 MB |            |
|                   HelloWorld.Console |           TrimmedReadyToRun |   26.34 ms |  14.27 MB |         NA |
|                   HelloWorld.Console | TrimmedReadyToRunCompressed |   47.07 ms |  11.40 MB |            |
|                   HelloWorld.Console |                         AOT |   10.17 ms |   0.97 MB |         NA |
|                                      |                             |            |           |            |
|              HelloWorld.HttpListener |               SelfContained |  117.83 ms |  69.46 MB |   32.03 MB |
|              HelloWorld.HttpListener |                     Trimmed |  275.14 ms |  12.96 MB |   27.11 MB |
|              HelloWorld.HttpListener |           TrimmedCompressed |  287.67 ms |  10.62 MB |   27.20 MB |
|              HelloWorld.HttpListener |           TrimmedReadyToRun |   96.63 ms |  20.36 MB |   26.02 MB |
|              HelloWorld.HttpListener | TrimmedReadyToRunCompressed |  131.53 ms |  14.05 MB |   35.07 MB |
|              HelloWorld.HttpListener |                         AOT |   55.04 ms |   8.72 MB |   14.59 MB |
|                                      |                             |            |           |            |
|                       HelloWorld.Web |               SelfContained |  233.89 ms |  94.23 MB |   50.56 MB |
|                       HelloWorld.Web |                     Trimmed |  507.58 ms |  17.28 MB |   40.39 MB |
|                       HelloWorld.Web |           TrimmedCompressed |  562.42 ms |  12.33 MB |   42.58 MB |
|                       HelloWorld.Web |           TrimmedReadyToRun |  158.79 ms |  37.93 MB |   43.45 MB |
|                       HelloWorld.Web | TrimmedReadyToRunCompressed |  260.63 ms |  20.85 MB |   67.05 MB |
|                       HelloWorld.Web |                         AOT |   72.97 ms |  21.86 MB |   36.15 MB |
|                                      |                             |            |           |            |
|              HelloWorld.Web.Stripped |               SelfContained |  173.00 ms |  94.23 MB |   40.77 MB |
|              HelloWorld.Web.Stripped |                     Trimmed |  348.29 ms |  14.49 MB |   32.32 MB |
|              HelloWorld.Web.Stripped |           TrimmedCompressed |  393.29 ms |  11.21 MB |   33.89 MB |
|              HelloWorld.Web.Stripped |           TrimmedReadyToRun |  118.70 ms |  25.01 MB |   33.15 MB |
|              HelloWorld.Web.Stripped | TrimmedReadyToRunCompressed |  186.30 ms |  15.88 MB |   47.11 MB |
|              HelloWorld.Web.Stripped |                         AOT |   51.85 ms |  12.22 MB |   20.92 MB |
|                                      |                             |            |           |            |
|             HelloWorld.KestrelDirect |               SelfContained |  191.51 ms |  93.88 MB |   42.07 MB |
|             HelloWorld.KestrelDirect |                     Trimmed |  332.89 ms |  13.93 MB |   32.30 MB |
|             HelloWorld.KestrelDirect |           TrimmedCompressed |  328.37 ms |  10.98 MB |   32.36 MB |
|             HelloWorld.KestrelDirect |           TrimmedReadyToRun |  128.09 ms |  23.20 MB |   32.77 MB |
|             HelloWorld.KestrelDirect | TrimmedReadyToRunCompressed |  165.78 ms |  15.19 MB |   43.38 MB |
|             HelloWorld.KestrelDirect |                         AOT |   60.37 ms |  11.16 MB |   18.58 MB |
|                                      |                             |            |           |            |
|    TrimmedTodo.Console.EfCore.Sqlite |               SelfContained |   508.1 ms |  75.78 MB |         NA |
|    TrimmedTodo.Console.EfCore.Sqlite |                     Trimmed |   740.5 ms |  20.78 MB |         NA |
|    TrimmedTodo.Console.EfCore.Sqlite |           TrimmedCompressed |   772.5 ms |  13.39 MB |         NA |
|    TrimmedTodo.Console.EfCore.Sqlite |           TrimmedReadyToRun |   249.0 ms |  48.85 MB |         NA |
|    TrimmedTodo.Console.EfCore.Sqlite | TrimmedReadyToRunCompressed |   368.3 ms |  25.42 MB |         NA |
|                                      |                             |            |           |            |
| TrimmedTodo.MinimalApi.Dapper.Sqlite |               SelfContained |   385.4 ms | 101.19 MB |   68.15 MB |
| TrimmedTodo.MinimalApi.Dapper.Sqlite |                     Trimmed |   935.4 ms |  32.05 MB |   60.37 MB |
| TrimmedTodo.MinimalApi.Dapper.Sqlite |           TrimmedCompressed | 1,066.9 ms |  17.81 MB |   72.09 MB |
| TrimmedTodo.MinimalApi.Dapper.Sqlite |           TrimmedReadyToRun |   286.4 ms |  69.05 MB |   65.18 MB |
| TrimmedTodo.MinimalApi.Dapper.Sqlite | TrimmedReadyToRunCompressed |   531.6 ms |  33.41 MB |  119.33 MB |
|                                      |                             |            |           |            |
| TrimmedTodo.MinimalApi.EfCore.Sqlite |               SelfContained |   644.8 ms | 104.78 MB |   80.48 MB |
| TrimmedTodo.MinimalApi.EfCore.Sqlite |                     Trimmed | 1,213.2 ms |  36.29 MB |   72.39 MB |
| TrimmedTodo.MinimalApi.EfCore.Sqlite |           TrimmedCompressed | 1,299.7 ms |  19.21 MB |   84.96 MB |
| TrimmedTodo.MinimalApi.EfCore.Sqlite |           TrimmedReadyToRun |   389.6 ms |  81.73 MB |   78.93 MB |
| TrimmedTodo.MinimalApi.EfCore.Sqlite | TrimmedReadyToRunCompressed |   623.0 ms |  38.40 MB |  143.16 MB |
|                                      |                             |            |           |            |
|        TrimmedTodo.MinimalApi.Sqlite |               SelfContained |   370.1 ms | 101.00 MB |   66.36 MB |
|        TrimmedTodo.MinimalApi.Sqlite |                     Trimmed |   929.8 ms |  31.82 MB |   58.97 MB |
|        TrimmedTodo.MinimalApi.Sqlite |           TrimmedCompressed |   983.5 ms |  17.70 MB |   70.68 MB |
|        TrimmedTodo.MinimalApi.Sqlite |           TrimmedReadyToRun |   297.6 ms |  68.33 MB |   63.72 MB |
|        TrimmedTodo.MinimalApi.Sqlite | TrimmedReadyToRunCompressed |   488.2 ms |  33.10 MB |  118.69 MB |
|                                      |                             |            |           |            |
|     TrimmedTodo.WebApi.EfCore.Sqlite |               SelfContained |   728.9 ms | 105.10 MB |   85.73 MB |
|     TrimmedTodo.WebApi.EfCore.Sqlite |                     Trimmed | 1,303.2 ms |  39.48 MB |   77.83 MB |
|     TrimmedTodo.WebApi.EfCore.Sqlite |           TrimmedCompressed | 1,543.9 ms |  20.54 MB |   90.39 MB |
|     TrimmedTodo.WebApi.EfCore.Sqlite |           TrimmedReadyToRun |   423.3 ms |  86.26 MB |   84.92 MB |
|     TrimmedTodo.WebApi.EfCore.Sqlite | TrimmedReadyToRunCompressed |   735.9 ms |  40.16 MB |  152.25 MB |
