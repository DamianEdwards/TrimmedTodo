$projectPath = ".\src\TrimmedTodo.Console.EfCore.Sqlite\TrimmedTodo.Console.EfCore.Sqlite.csproj";
$projectName = "TodoConsole";
$artifacts = ".artifacts";
$rid = "win-x64";

Write-Host "Cleaning up previous run";
Get-ChildItem "$artifacts\$projectName" | Remove-Item -Recurse -Force;
dotnet clean -v q --nologo

Write-Host "Publishing ${projectName}: dotnet publish -c Release -r $rid --self-contained";
dotnet publish $projectPath -c Release -r $rid --self-contained -v m --nologo -o "$artifacts\$projectName"
Write-Host;

$appSize = ((Get-ChildItem "$artifacts\$projectName\*.exe")[0].Length / (1024 * 1024)).ToString("#.##");
Write-Host "App executable size: $appSize MB";

Write-Host "Running $projectName";
Write-Host;
& ".\$artifacts\$projectName\TrimmedTodo.Console.EfCore.Sqlite.exe"