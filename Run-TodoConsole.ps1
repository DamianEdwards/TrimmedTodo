$projectPath = ".\src\TrimmedTodo.Console.EfCore.Sqlite\TrimmedTodo.Console.EfCore.Sqlite.csproj";
$projectName = "TodoConsole";
$artifacts = ".artifacts";
$rid = "win-x64";

Write-Host "Cleaning up previous run";
Get-ChildItem "$artifacts\$projectName" | Remove-Item -Recurse -Force;
dotnet clean -v q --nologo
Get-ChildItem -Path "bin" -Recurse | Remove-Item -Recurse -Force;
Get-ChildItem -Path "obj" -Recurse | Remove-Item -Recurse -Force;

Write-Host "Publishing ${projectName}: dotnet publish -c Release -r $rid --self-contained";
dotnet publish $projectPath -c Release -r $rid --self-contained -v m --nologo -o "$artifacts\$projectName" /p:PublishTrimmed=true /p:PublishSingleFile=true
#dotnet publish $projectPath -c Release -r $rid --self-contained -v m --nologo -o "$artifacts\$projectName" /p:PublishTrimmed=true
#dotnet publish $projectPath -c Release -r $rid --self-contained -v m --nologo -o "$artifacts\$projectName" /p:PublishTrimmed=true
#dotnet publish $projectPath -c Release -r $rid --self-contained -v m --nologo -o "$artifacts\$projectName" /p:PublishAot=true
Write-Host;

$appExe = (Get-ChildItem "$artifacts\$projectName\*.exe")[0];
$appSize = ($appExe[0].Length / (1024 * 1024)).ToString("#.##");
$appAccessTime = $appExe.LastAccessTime;
Write-Host "App executable size: $appSize MB";
Write-Host "App last access time: $appAccessTime";
Write-Host;

Write-Host "Running $projectName";
Write-Host;
& ".\$artifacts\$projectName\TrimmedTodo.Console.EfCore.Sqlite.exe"