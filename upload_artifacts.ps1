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
} else {
    Write-Host "Warning: .env file not found. Make sure to create one with the necessary environment variables."
}

# Archive thirdparties from Assets folder into a zip file
$zipFilePath = "Assets.zip"
$zipStagingPath = Join-Path ([System.IO.Path]::GetTempPath()) ("ThirteenthRoomAssetsZip_" + [System.Guid]::NewGuid().ToString("N"))

if (Test-Path $zipFilePath) {
    Remove-Item $zipFilePath
}

try {
    New-Item -Path (Join-Path $zipStagingPath "Plugins") -ItemType Directory -Force | Out-Null
    Copy-Item -Path "Assets/Plugins/Demigiant" -Destination (Join-Path $zipStagingPath "Plugins/Demigiant") -Recurse
    Copy-Item -Path "Assets/Thirdparty" -Destination (Join-Path $zipStagingPath "Thirdparty") -Recurse

    Compress-Archive -Path (Join-Path $zipStagingPath "*") -DestinationPath $zipFilePath
}
finally {
    if (Test-Path $zipStagingPath) {
        Remove-Item $zipStagingPath -Recurse -Force
    }
}

# Extract the version number from artifacts_version.txt
$versionFilePath = "artifacts_version.txt"
if (Test-Path $versionFilePath) {
    $version = Get-Content $versionFilePath -Raw
    $version = $version.Trim()
} else {
    Write-Host "Error: artifacts_version.txt file not found. Please create one with the version number."
    exit 1
}

if (-not $version) {
    Write-Host "Error: Version number is empty. Please check artifacts_version.txt."
    exit 1
}

# Upload the Assets.zip file to Nexus Repository Manager using PowerShell
Write-Output "Uploading Assets.zip version $version to Nexus Repository Manager..."

$loginPassword = "{0}:{1}" -f $env:NEXUS_USERNAME, $env:NEXUS_PASSWORD
$encodedCredentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($loginPassword))
$headers = @{ Authorization = "Basic " + $encodedCredentials }

function Upload-Artifact {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Write-Output "Uploading to $Uri"
    $response = Invoke-WebRequest `
        -Uri $Uri `
        -Headers $headers `
        -Method Put `
        -InFile $Path `
        -ContentType "application/zip" `
        -UseBasicParsing `
        -DisableKeepAlive `
        -ErrorAction Stop

    Write-Output "Upload completed with HTTP $($response.StatusCode) $($response.StatusDescription)"
}

Upload-Artifact -Uri "$env:NEXUS_URL/repository/teamcity-raw/thirteenthroom/$version/Assets.zip" -Path "Assets.zip"
Upload-Artifact -Uri "$env:NEXUS_URL/repository/teamcity-raw/thirteenthroom/latest/Assets.zip" -Path "Assets.zip"
