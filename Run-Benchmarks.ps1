$ErrorActionPreference = 'Stop';

$projectName = "Benchmarks";
$projectDir = "tests\$projectName";
$projectPath = "$projectDir\$projectName.csproj";
$artifacts = ".artifacts";

Write-Host "Cleaning up previous run";

#Write-Host "dotnet restore $projectPath /p:Configuration=Release"
#dotnet restore $projectPath /p:Configuration=Release

Write-Host "dotnet clean $projectPath -c Release -v q --nologo -o "$artifacts\$projectName""
dotnet clean $projectPath -c Release -v q --nologo -o "$artifacts\$projectName"

Get-ChildItem -Include "bin\Release" -Recurse -Directory | Remove-Item -Recurse -Force;
Get-ChildItem -Include "obj\Release" -Recurse -Directory | Remove-Item -Recurse -Force;
if (Test-Path -Path "$artifacts\$projectName") {
    Get-ChildItem -Path "$artifacts\$projectName" | Remove-Item -Recurse -Force;
}

Write-Host "Building ${projectName}: dotnet build -c Release";
dotnet build $projectPath -c Release -v m --nologo -o "$artifacts\$projectName"

if ($LASTEXITCODE -ne 0 -or (Test-Path -Path "$artifacts\$projectName\$projectName.exe") -ne $true)
{
    Write-Error "Building failed, see error above";
    return;
}

$appExe = Get-ChildItem -Path "$artifacts\$projectName\$projectName.exe";
$appAccessTime = $appExe.LastAccessTime;
Write-Host "Exe last access time: $appAccessTime";
Write-Host;

Write-Host "Running $appExe";
Write-Host;

Start-Process -FilePath ".\$artifacts\$projectName\$projectName.exe" -WorkingDirectory ".\$artifacts\$projectName" -NoNewWindow -Wait
