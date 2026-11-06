param([Parameter(Mandatory=$true)][string]$UnityEditorDirectory)
$ErrorActionPreference = 'Stop'
$projectDirectory = Split-Path $PSScriptRoot -Parent
Push-Location $projectDirectory
try {
    $dataDirectory = Join-Path $UnityEditorDirectory 'Data'
    $dotnetPath = Join-Path $dataDirectory 'NetCoreRuntime/dotnet.exe'
    $compilerPath = Join-Path $dataDirectory 'DotNetSdkRoslyn/csc.dll'
    $monoPath = Join-Path $dataDirectory 'MonoBleedingEdge/bin/mono.exe'
    $referencePath = Join-Path $dataDirectory 'MonoBleedingEdge/lib/mono/4.7.1-api/mscorlib.dll'
    New-Item -ItemType Directory -Path Logs -Force | Out-Null
    & $dotnetPath $compilerPath -nologo -nostdlib -define:UNITY_WEBGL -target:exe -out:Logs/PortalServiceHarness.exe "-r:$referencePath" Tests/PortalServiceHarness.cs Assets/DontSplash/Scripts/PortalService.cs Assets/DontSplash/Scripts/GameSave.cs
    if ($LASTEXITCODE -ne 0) { throw 'Regression harness compilation failed' }
    & $monoPath Logs/PortalServiceHarness.exe
    if ($LASTEXITCODE -ne 0) { throw 'Portal/save regression checks failed' }
} finally { Pop-Location }
