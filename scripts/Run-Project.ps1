param (
    [Parameter()]
    [string]
    $ProjectName,
    [Parameter()]
    [string]
    $Rid = "win-x64"
)

#Requires -Version 7.0

Get-Command dotnet -ErrorAction Stop | Out-Null;

#TODO: Parse current platform RID from `dotnet --info` and default $Rid to that

$ErrorActionPreference = 'Stop';

$projectDir = ".\src\$projectName";
$projectPath = "$projectDir\$projectName.csproj";
$artifacts = ".artifacts";

Write-Host "Cleaning up previous run";
if (Test-Path -Path "$artifacts\$projectName") {
    Get-ChildItem -Path "$artifacts\$projectName" | Remove-Item -Recurse -Force;
}
dotnet clean -v q --nologo
Get-ChildItem -Path "bin" -Recurse | Remove-Item -Recurse -Force;
Get-ChildItem -Path "obj" -Recurse | Remove-Item -Recurse -Force;

Write-Host "Publishing ${projectName}: dotnet publish -c Release -r $Rid --self-contained";
dotnet publish $projectPath -c Release -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName" /p:PublishTrimmed=true /p:PublishSingleFile=true
#dotnet publish $projectPath -c Release -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName" /p:PublishTrimmed=true
#dotnet publish $projectPath -c Release -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName" /p:PublishTrimmed=true
#dotnet publish $projectPath -c Release -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName" /p:PublishAot=true
Write-Host;

$appExe = Get-ChildItem -Path "$artifacts\$projectName\$projectName.exe";
$appSize = ($appExe.Length / (1024 * 1024)).ToString("#.##");
$appAccessTime = $appExe.LastAccessTime;
Write-Host "App executable size: $appSize MB";
Write-Host "App last access time: $appAccessTime";
Write-Host;

Write-Host "Running $projectName";
Write-Host;

Start-Process -FilePath ".\$artifacts\$projectName\$projectName.exe" -WorkingDirectory ".\$artifacts\$projectName" -NoNewWindow -Wait