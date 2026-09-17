$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$nativeDir = Join-Path $projectDir "src\native"
$binDir = Join-Path $projectDir "bin"
$outputExe = Join-Path $binDir "WinComputerUseHelper.exe"

if (-not (Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir -Force | Out-Null
}

$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    $cscPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $cscPath)) {
    Write-Error "Could not find csc.exe compiler"
    exit 1
}

$wpfLib = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
if (-not (Test-Path $wpfLib)) {
    $wpfLib = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF"
}

$sourceFiles = (Get-ChildItem -Path $nativeDir -Filter "*.cs").FullName
Write-Host "Compiling $($sourceFiles.Count) C# files -> $outputExe"

$compilerArgs = @(
    "/nologo",
    "/target:exe",
    "/optimize+",
    "/lib:$wpfLib",
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Web.Extensions.dll",
    "/r:UIAutomationClient.dll",
    "/r:UIAutomationTypes.dll",
    "/r:WindowsBase.dll",
    "/out:$outputExe"
) + $sourceFiles

& $cscPath $compilerArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Compilation failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Compilation successful: $outputExe"
