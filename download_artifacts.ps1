
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

# Download the Assets.zip file from Nexus Repository Manager using PowerShell
Invoke-WebRequest -Uri "$env:NEXUS_URL/repository/teamcity-raw/thirteenthroom/Assets.zip" -Headers @{ Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$env:NEXUS_USERNAME:$env:NEXUS_PASSWORD")) } -OutFile "Assets.zip"

# Extract the downloaded zip file
$extractPath = "Assets"
Expand-Archive -Path "Assets.zip" -DestinationPath $extractPath -Force