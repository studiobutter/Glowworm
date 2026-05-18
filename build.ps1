param(
    [string] $Architecture = "x64",
    [string] $Version = "0.1.1",
    [string] $Output = "build/Glowworm"
)

$ErrorActionPreference = "Stop";

$env:Path += ';C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\';

if (-not (Test-Path $Output)) {
    New-Item -ItemType Directory -Force $Output
}

$NumericVersion = $Version -replace '[^0-9.]', ''
if ($NumericVersion -match '^\d+\.\d+\.\d+$') {
    $NumericVersion += ".0"
}

dotnet publish src/Glowworm -c Release -r "win-$Architecture" -o "$Output/app-$Version" -p:Platform=$Architecture -p:Version=$NumericVersion;

if (Test-Path "src/Glowworm.Launcher") {
    msbuild src/Glowworm.Launcher "-property:Configuration=Release;Platform=$Architecture;OutDir=$(Resolve-Path "$Output/")";
}

Add-Content "$Output/version.ini" -Value "exe_path=app-$Version\Glowworm.exe" -Force;

if (Test-Path "$Output/app-$Version/Glowworm.pdb") {
    Remove-Item "$Output/app-$Version/Glowworm.pdb" -Force;
}

