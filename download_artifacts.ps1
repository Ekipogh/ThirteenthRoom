# Load environment variables from .env file
$envFilePath = ".env"
if (Test-Path $envFilePath) {
    Get-Content $envFilePath | ForEach-Object {
        if ($_ -match "^\s*([^=]+)\s*=\s*(.+)\s*$") {
            $name = $matches[1]
            $value = $matches[2]
            [System.Environment]::SetEnvironmentVariable($name, $value)
        }
    }
}
else {
    Write-Host "Warning: .env file not found. Make sure to create one with the necessary environment variables."
}

$CurrentVersion = Get-Content ".\artifacts_version.txt" -Raw
$CurrentVersion = $CurrentVersion.Trim()

if ([string]::IsNullOrEmpty($CurrentVersion)) {
    Write-Host "Error: Current version is empty. Please ensure artifacts_version.txt contains the correct version."
    exit 1
}

$CacheDir = if ($env:CACHE_DIR) { $env:CACHE_DIR } else { ".\.tc-cache\artifacts" }
$CachedVersionFile = "$CacheDir\artifacts_version.txt"
$ZipPath = "$CacheDir\Assets.zip"
$ExtractDir = ".\Assets"

New-Item -ItemType Directory -Force -Path $CacheDir | Out-Null

$CachedVersion = ""
if (Test-Path $CachedVersionFile) {
    $CachedVersion = (Get-Content $CachedVersionFile -Raw).Trim()
}

if ($CurrentVersion -eq $CachedVersion -and (Test-Path $ExtractDir)) {
    Write-Host "Artifacts version $CurrentVersion already cached. Skipping download."
}
else {
    Write-Host "Artifacts changed: cached='$CachedVersion', current='$CurrentVersion'. Downloading..."

    $url = "$env:NEXUS_URL/repository/teamcity-raw/thirteenthroom/$CurrentVersion/Assets.zip"
    Write-Host "Downloading artifacts from $url"

    $loginPassword = "{0}:{1}" -f $env:NEXUS_USERNAME, $env:NEXUS_PASSWORD
    $encodedCredentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($loginPassword))
    $headers = @{ Authorization = "Basic " + $encodedCredentials }

    Invoke-WebRequest -Uri $url `
        -OutFile $ZipPath `
        -Headers $headers
}
// Extract the zip file

Test-Path $ZipPath -ErrorAction Stop

Expand-Archive -Path $ZipPath -DestinationPath $ExtractDir -Force

Set-Content -Path $CachedVersionFile -Value $CurrentVersion