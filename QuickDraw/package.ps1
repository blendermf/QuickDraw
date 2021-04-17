param(
    [String]$PackageName = $(throw "-PackageName is required."),
    [String]$PackageVersion = $(throw "-PackageVersion is required."),
    [String]$TargetDir = $(throw "-TargetDir is required.")
)

$zipPath = ([io.path]::combine($TargetDir, $PackageName + "_v" + $PackageVersion + ".zip" ));

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

$tmpDir = Join-Path $TargetDir $([io.path]::GetRandomFileName());
New-Item -ItemType directory -Path $tmpDir -Force;
Get-ChildItem -Path $TargetDir | % { Copy-Item -Path $_.FullName -Destination $tmpDir -Recurse -Force -Exclude "*.runtimeconfig.dev.json"};
Compress-Archive -Force -Path $tmpDir/* -DestinationPath $zipPath;
Remove-Item -Recurse -Force $tmpDir; 