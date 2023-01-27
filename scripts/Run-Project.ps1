param (
    [Parameter()]
    [string]
    $ProjectName,
    [Parameter()]
    [string]
    $Rid = ""
)

#Requires -Version 7.0

Get-Command dotnet -ErrorAction Stop | Out-Null;

# Parse current platform RID from `dotnet --info` and default $Rid to that
if ($Rid -eq "") {
    $Rid = (dotnet --info | Select-String "(?:RID:\s+)(\w+\-\w+)").Matches[0].Groups[1];
}

#$ErrorActionPreference = 'Stop';

$projectDir = ".\src\$projectName";
$projectPath = "$projectDir\$projectName.csproj";
$artifacts = ".artifacts";

Write-Host "Cleaning up previous run";

Write-Host "dotnet restore $projectPath -r $Rid -p:Configuration=Debug"
dotnet restore $projectPath -r $Rid -p:Configuration=Debug

Write-Host "dotnet restore $projectPath -r $Rid -p:Configuration=Release"
dotnet restore $projectPath -r $Rid -p:Configuration=Release

Write-Host "dotnet clean $projectPath -c Debug -v q --nologo -o "$artifacts\$projectName""
dotnet clean $projectPath -c Debug -v q --nologo -o "$artifacts\$projectName"

Write-Host "dotnet clean $projectPath -c Release -r $Rid -v q --nologo -o "$artifacts\$projectName""
dotnet clean $projectPath -c Release -v q --nologo -o "$artifacts\$projectName"

#Get-ChildItem -Include bin -Recurse -Directory | Remove-Item -Recurse -Force;
#Get-ChildItem -Include obj -Recurse -Directory | Remove-Item -Recurse -Force;
if (Test-Path -Path "$artifacts\$projectName") {
    Get-ChildItem -Path "$artifacts\$projectName" | Remove-Item -Recurse -Force;
}

Write-Host "Publishing ${projectName}: dotnet publish -r $Rid --self-contained";
dotnet publish $projectPath -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName"
#dotnet publish $projectPath -c Release -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName"
#dotnet publish $projectPath -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName" -p:PublishTrimmed=true -p:PublishSingleFile=true
#dotnet publish $projectPath -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName" -p:PublishTrimmed=true
#dotnet publish $projectPath -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName" -p:PublishTrimmed=true
#dotnet publish $projectPath -r $Rid --self-contained -v m --nologo -o "$artifacts\$projectName" -p:PublishAot=true -p:PublishSingleFile=false
Write-Host;

if ($LASTEXITCODE -ne 0 -or (Test-Path -Path "$artifacts\$projectName\$projectName.exe") -ne $true)
{
    Write-Error "Publish failed, see error above";
    return;
}

$appExe = Get-ChildItem -Path "$artifacts\$projectName\$projectName.exe";
if (Test-Path -Path "$artifacts\$projectName\$projectName.dll") {
    $appSize = (Get-ChildItem -Path "$artifacts\$projectName\" -Exclude *.pdb | Measure-Object -Property Length -Sum).Sum;
    $appSize = "{0:N2} MB" -f ($appSize / 1MB);
    Write-Host "App was published as multiple files totaling $appSize";
    Write-Host "Top 20 largest files:";
    Get-ChildItem -Path "$artifacts\$projectName\" | Sort-Object -Property Length -Descending
        | Select-Object -First 20 -Property Name,Length
            | Format-Table Name, @{Label='Size';Expression={"{0:N2} KB" -f ($_.Length/1KB)};Alignment='right'};
}
else {
    $appSize = $appExe.Length;
    $appSize = "{0:N2} MB" -f ($appSize / 1MB);
    Write-Host "App was published as a single $appSize file ($projectName.exe)";
    Write-Host;
}

$appAccessTime = $appExe.LastAccessTime;
Write-Host "App last access time: $appAccessTime";
Write-Host;

Write-Host "Running $projectName";
Write-Host;

Start-Process -FilePath ".\$artifacts\$projectName\$projectName.exe" -WorkingDirectory ".\$artifacts\$projectName" -NoNewWindow -Wait
