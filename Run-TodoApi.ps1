$ErrorActionPreference = 'Stop';

$projectName = "TrimmedTodo.MinimalApi.EfCore.Sqlite";

# Set env var for JWT signing key
$configKeyName = "dotnet-user-jwts:KeyMaterial";
$userSecretsId = "7deba5fe-f06c-4a7d-9816-cdeccfc4e4a8";
$userSecretsJsonPath = $env:APPDATA + "\Microsoft\UserSecrets\$userSecretsId\secrets.json";
if ((Test-Path -Path $userSecretsJsonPath) -ne $true)
{
    Write-Error "API project '$projectName' has not been initialized for JWT authentication.`r`nPlease run 'dotnet user-jwts create' in the API project directory.";
    return;
}
$userSecrets = Get-Content -Path $userSecretsJsonPath | ConvertFrom-Json;
$jwtSigningKey = $userSecrets | Select-Object -Property $configKeyName;
$env:JWT_SIGNING_KEY = $jwtSigningKey.($configKeyName);

.\scripts\Run-Project.ps1 -ProjectName $projectName