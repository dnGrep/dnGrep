param(
    #Path to AssemblyVersionCommon.cs
    $AssemblyVersionCommonCSPath,
    #Path to .msi
    $TargetPath,
    #Path to .wixpdb
    $TargetPdbPath
)
Import-Module "$PSScriptRoot\ParseVersion.psm1" -Force

Write-Host "Renaming .msi and .wipdb to have vesion number in filenames"
function rename($filePath, $version)
{
    $file = Get-Item $filePath
    $newFileName = ( ($file.BaseName).replace('X.X.X.X',"$($version.MajorVersion).$($version.MinorVersion).$($version.BuildVersion)") + $file.Extension )
    Move-Item $filePath (Join-Path $file.Directory.FullName $newFileName)
}
$version = ParseVersion $AssemblyVersionCommonCSPath
rename $TargetPath $version
rename $TargetPdbPath $version