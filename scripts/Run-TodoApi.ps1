$ErrorActionPreference = 'Stop';

# Set env var for urls
$env:ASPNETCORE_URLS = "http://localhost:5079";

# Set env var for JWT signing key
$configKeyName = "Authentication:Schemes:Bearer:SigningKeys";
$userSecretsId = "7deba5fe-f06c-4a7d-9816-cdeccfc4e4a8";
$userSecretsJsonPath = $env:APPDATA + "\Microsoft\UserSecrets\$userSecretsId\secrets.json";
if ((Test-Path -Path $userSecretsJsonPath) -ne $true)
{
    Write-Error "API project '$projectName' has not been initialized for JWT authentication.`r`nPlease run 'dotnet user-jwts create' in the '$projectName' directory.";
    return;
}

$userSecretsJson = Get-Content -Path $userSecretsJsonPath | ConvertFrom-Json;
#$userSecretsJson | Format-Table;

$jwtSigningKeysArray = $userSecretsJson.($configKeyName);
#Write-Host $jwtSigningKeysArray;

$dotnetUserJwtsKeys = $jwtSigningKeysArray.Where({$_.("Issuer") -eq "dotnet-user-jwts"});
#Write-Host $dotnetUserJwtsKeys;

if ($dotnetUserJwtsKeys.Count -eq 1) {
    $env:JWT_SIGNING_KEY = $dotnetUserJwtsKeys[0].("Value");
    #Write-Host $dotnetUserJwtsKeys[0].("Value");

    .\scripts\Run-Project.ps1 -ProjectName $projectName;
} else {
    Write-Error "Could not find signing key for issuer 'dotnet-user-jwts'";
    return;
}
