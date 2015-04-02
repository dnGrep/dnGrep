param(
    #Path to AssemblyVersionCommon.cs
    $AssemblyVersionCommonCSPath,
    #Path to Variables.wxi
    $VariablesWxi
)
Import-Module "$PSScriptRoot\ParseVersion.psm1" -Force

Write-Host "Putting version number from `"$AssemblyVersionCommonCSPath`" to `"$VariablesWxi`""

$version = ParseVersion $AssemblyVersionCommonCSPath

# Write the version number into the MSI name
$xml = [xml](Get-Content $VariablesWxi)
$MajorVersionXml = $xml.Include.ChildNodes | where {$_.Value.contains('MajorVersion')}
$MinorVersionXml = $xml.Include.ChildNodes | where {$_.Value.contains('MinorVersion')}
$BuildVersionXml = $xml.Include.ChildNodes | where {$_.Value.contains('BuildVersion')}
$RevisionXml     = $xml.Include.ChildNodes | where {$_.Value.contains('Revision')}
$MajorVersionXml = $version.MajorVersion
$MinorVersionXml = $version.MinorVersion
$BuildVersionXml = $version.BuildVersion
$RevisionXml     = $version.Revision

$xml.Save($VariablesWxi)