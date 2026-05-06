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
$3rdPartyList = @(
    "Assets/Plugins",
    "Assets/Synty",
    "Assets/FPSFont",
    "Assets/PolygonHorrorMansion",
    "Assets/Microlight",
    "Assets/TextMesh Pro",
    "Assets/Kevin Iglesias",
    "Assets/Voices - Essentials",
    "Assets/Footsteps - Essentials"
)
$zipFilePath = "Assets.zip"
if (Test-Path $zipFilePath) {
    Remove-Item $zipFilePath
}
Compress-Archive -Path $3rdPartyList -DestinationPath $zipFilePath


# Upload the Assets.zip file to Nexus Repository Manager using PowerShell
Write-Output "Uploading Assets.zip to Nexus Repository Manager..."

$loginPassword = "{0}:{1}" -f $env:NEXUS_USERNAME, $env:NEXUS_PASSWORD
$encodedCredentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($loginPassword))

Invoke-WebRequest -Uri "$env:NEXUS_URL/repository/teamcity-raw/thirteenthroom/Assets.zip" -Headers @{ Authorization = "Basic " + $encodedCredentials } -Method Put -InFile "Assets.zip" -ContentType "application/zip" -UseBasicParsing
