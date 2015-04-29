param(
    #Path to AssemblyVersionCommon.cs
    $AssemblyVersionCommonCSPath,
    #Path to Variables.wxi
    $VariablesWxi
)
Import-Module "$PSScriptRoot\ParseVersion.psm1" -Force

# Utility function. Given the digit grouping, xml, and number value, sets the appropriate Wix variable in the xml.
function setVersion($groupName, $number, $xml)
{
    $nodeXml = $xml.Include.ChildNodes | where {($_.NodeType -eq 'ProcessingInstruction') -and ($_.InnerText -match "$groupName\s*=\s*`"")}
    $nodeXml.InnerText = "$groupName=`"$number`" "
}

Write-Host "Putting version number from `"$AssemblyVersionCommonCSPath`" to `"$VariablesWxi`""

$version = ParseVersion $AssemblyVersionCommonCSPath

# Write the version number into the MSI name
$xml = [xml](Get-Content $VariablesWxi)

setVersion 'MajorVersion' $version.MajorVersion $xml
setVersion 'MinorVersion' $version.MinorVersion $xml
setVersion 'BuildVersion' $version.BuildVersion $xml
setVersion 'Revision'     $version.Revision     $xml

$xml.Save($VariablesWxi)