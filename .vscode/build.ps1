Param(
    [string]$Project,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = 'Stop'

function Find-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $path = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($path) { return $path }
    }

    $candidates = @(
        'C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }

    return $null
}

if (-not (Test-Path $Project)) {
    Write-Error "Project file not found: $Project"
}

$msbuild = Find-MSBuild
if (-not $msbuild) {
    Write-Error "MSBuild.exe not found. Install Visual Studio Build Tools 2022 or Visual Studio and ensure MSBuild is installed."
}

& "$msbuild" $Project "/t:Build" "/p:Configuration=$Configuration" "/v:m"
exit $LASTEXITCODE


