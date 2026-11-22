param(
    [string]$Version,
    [string]$BuildNumber
)

$ErrorActionPreference = "Stop"
$ExitCode = 0
$ZipPath = $null

try {
    if (-not $Version -or -not $BuildNumber) {
        throw "Usage: .\upload_build.ps1 <version> <build-number>"
    }

    foreach ($value in @($Version, $BuildNumber)) {
        if ($value -notmatch "^[A-Za-z0-9][A-Za-z0-9._-]*$") {
            throw "Version and build number must contain only letters, numbers, periods, underscores, and hyphens."
        }
    }

    $envFilePath = ".env"
    if (Test-Path $envFilePath) {
        Get-Content $envFilePath | ForEach-Object {
            if ($_ -match "^\s*([^=]+)\s*=\s*(.+)\s*$") {
                [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
            }
        }
    }

    foreach ($name in @("NEXUS_URL", "NEXUS_USERNAME", "NEXUS_PASSWORD")) {
        if ([string]::IsNullOrWhiteSpace([System.Environment]::GetEnvironmentVariable($name))) {
            throw "Required environment variable $name is not set."
        }
    }

    $BuildDir = Join-Path (Get-Location).Path "ThirteensRoom"
    if (-not (Test-Path -LiteralPath $BuildDir -PathType Container)) {
        throw "Build directory not found: $BuildDir"
    }
    if (@(Get-ChildItem -LiteralPath $BuildDir -Force).Count -eq 0) {
        throw "Build directory is empty: $BuildDir"
    }

    $ZipPath = Join-Path ([System.IO.Path]::GetTempPath()) ("ThirteensRoom_" + [System.Guid]::NewGuid().ToString("N") + ".zip")
    Compress-Archive -Path (Join-Path $BuildDir "*") -DestinationPath $ZipPath -Force

    $baseUrl = $env:NEXUS_URL.TrimEnd("/")
    $escapedVersion = [Uri]::EscapeDataString($Version)
    $escapedBuildNumber = [Uri]::EscapeDataString($BuildNumber)
    $url = "$baseUrl/repository/teamcity-raw/thirteenthroom/releases/windows/$escapedVersion/$escapedBuildNumber/ThirteensRoom.zip"

    $loginPassword = "{0}:{1}" -f $env:NEXUS_USERNAME, $env:NEXUS_PASSWORD
    $encodedCredentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($loginPassword))
    $headers = @{ Authorization = "Basic " + $encodedCredentials }

    Write-Output "Uploading build $Version ($BuildNumber) to $url"
    $response = Invoke-WebRequest `
        -Uri $url `
        -Headers $headers `
        -Method Put `
        -InFile $ZipPath `
        -ContentType "application/zip" `
        -UseBasicParsing `
        -ErrorAction Stop

    Write-Output "Upload completed with HTTP $($response.StatusCode) $($response.StatusDescription)"
}
catch {
    [Console]::Error.WriteLine("Build upload failed: $($_.Exception.Message)")
    $ExitCode = 1
}
finally {
    if ($ZipPath -and (Test-Path -LiteralPath $ZipPath)) {
        try {
            Remove-Item -LiteralPath $ZipPath -Force -ErrorAction Stop
        }
        catch {
            [Console]::Error.WriteLine("Failed to remove temporary archive: $($_.Exception.Message)")
            $ExitCode = 1
        }
    }
}

exit $ExitCode
