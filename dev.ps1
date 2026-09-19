<#
.SYNOPSIS
Builds and runs the API and the Blazor client together in the current terminal.

.DESCRIPTION
Builds both app projects once, then starts each with `dotnet run --no-build` using its `https`
launch profile, so their logs interleave in this terminal. Once both apps accept connections, the
script prints their URLs. Press Ctrl+C to stop both; if either app exits on its own, the other is
stopped too.

Both projects are built up front, one after the other, because they share the Contracts project
and two concurrent builds would race on its obj/ folder.

Works in Windows PowerShell 5.1 and PowerShell 7+.

.PARAMETER OpenBrowser
Opens the Blazor client in the default browser once it is ready.

.PARAMETER StartupTimeoutSeconds
How long to wait for each app to start accepting connections before giving up. Defaults to 60.

.EXAMPLE
./dev.ps1

.EXAMPLE
./dev.ps1 -OpenBrowser
#>
[CmdletBinding()]
param(
    [switch]$OpenBrowser,
    [int]$StartupTimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'

$apiDir = Join-Path $PSScriptRoot 'Api/src/EntityDetails.Api'
$clientDir = Join-Path $PSScriptRoot 'BlazorClient/src/EntityDetails.BlazorClient'
$launchProfile = 'https'

# Reads the first URL of the launch profile, so the printed URLs and the readiness check always
# match what launchSettings.json actually configures.
function Get-LaunchProfileUrl([string]$ProjectDir) {
    $settings = Get-Content -Raw (Join-Path $ProjectDir 'Properties/launchSettings.json') | ConvertFrom-Json
    return [Uri]($settings.profiles.$launchProfile.applicationUrl -split ';')[0]
}

function Start-App([string]$Name, [string]$ProjectDir) {
    Write-Host "Starting $Name..." -ForegroundColor Cyan
    # The project directory is the working directory so the app finds its appsettings files and,
    # for the API, creates its SQLite database in the same place a plain `dotnet run` would.
    return Start-Process -FilePath 'dotnet' `
        -ArgumentList @('run', '--no-build', '--launch-profile', $launchProfile) `
        -WorkingDirectory $ProjectDir -NoNewWindow -PassThru
}

# Polls until the app's port accepts a TCP connection. A TCP check avoids HTTPS certificate
# validation, which fails in Windows PowerShell when the dev certificate isn't trusted.
function Wait-AppReady([string]$Name, [Uri]$Url, [System.Diagnostics.Process]$Process) {
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ($Process.HasExited) {
            throw "$Name exited during startup."
        }

        $tcp = New-Object System.Net.Sockets.TcpClient
        try {
            $tcp.Connect($Url.Host, $Url.Port)
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
        finally {
            $tcp.Dispose()
        }
    }

    throw "$Name did not start listening on $Url within $StartupTimeoutSeconds seconds."
}

# `dotnet run` launches the app as a child process, so stopping only the dotnet PID would leave
# the app running and holding its port. Kill the whole tree instead.
function Stop-AppTree([System.Diagnostics.Process]$Process) {
    if ($null -eq $Process -or $Process.HasExited) {
        return
    }

    if ($PSVersionTable.PSEdition -eq 'Core') {
        $Process.Kill($true)
    }
    else {
        # Windows PowerShell runs on .NET Framework, whose Process.Kill() has no tree option.
        # taskkill reports already-exited children on stderr, which Windows PowerShell would turn
        # into a terminating error under the script-wide 'Stop' preference.
        $ErrorActionPreference = 'SilentlyContinue'
        taskkill.exe /PID $Process.Id /T /F 2>&1 | Out-Null
    }
}

$apiUrl = Get-LaunchProfileUrl $apiDir
$clientUrl = Get-LaunchProfileUrl $clientDir

dotnet dev-certs https --check --trust | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Warning ('The HTTPS development certificate is missing or untrusted. Run ' +
        '`dotnet dev-certs https --trust` or the browser will reject the API and client.')
}

foreach ($dir in @($apiDir, $clientDir)) {
    dotnet build $dir
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $dir."
    }
}

$api = $null
$client = $null
$exitCode = 0
try {
    $api = Start-App 'API' $apiDir
    $client = Start-App 'Blazor client' $clientDir

    Wait-AppReady 'API' $apiUrl $api
    Wait-AppReady 'Blazor client' $clientUrl $client

    Write-Host ''
    Write-Host "API:           $apiUrl" -ForegroundColor Green
    Write-Host "OpenAPI:       $([Uri]::new($apiUrl, 'openapi/v1.json'))" -ForegroundColor Green
    Write-Host "Blazor client: $clientUrl" -ForegroundColor Green
    Write-Host 'Press Ctrl+C to stop both.' -ForegroundColor Green
    Write-Host ''

    if ($OpenBrowser) {
        Start-Process $clientUrl.AbsoluteUri
    }

    while (-not $api.HasExited -and -not $client.HasExited) {
        Start-Sleep -Milliseconds 500
    }

    $stopped = if ($api.HasExited) { 'API' } else { 'Blazor client' }
    Write-Warning "$stopped exited; stopping the other app."
    $exitCode = 1
}
catch {
    Write-Error $_ -ErrorAction Continue
    $exitCode = 1
}
finally {
    # Also runs on Ctrl+C, which PowerShell turns into a pipeline stop rather than a catchable error.
    Stop-AppTree $client
    Stop-AppTree $api
}

exit $exitCode
