param( [string]$workingDirectory = $PSScriptRoot )
Write-Host 'Executing Powershell script make-msix.ps1 with working directory set to: ' $workingDirectory
Set-Location $workingDirectory

Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" | Select Name

$version = $env:APPVEYOR_BUILD_VERSION
Write-Host 'New version is: ' $version

[Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq")
$path = $workingDirectory + "/dnGREP.ContextMenuPkg/AppxManifest.xml"
$doc = [System.Xml.Linq.XDocument]::Load($path)
$xName = [System.Xml.Linq.XName]"{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity"
$doc.Root.Element($xName).Attribute("Version").Value = $version;
$doc.Save($path)
Write-Host 'Updated version in ' $path

$packagePath = $workingDirectory + "/dnGREP.ContextMenuPkg"
$msixPath = $workingDirectory + "/dnGREP.msix"
#$args = "pack /o /d `"$packagePath`" /p `"$msixPath`" /nv"
& 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe' @('pack', '/o', '/d', $packagePath, '/p', $msixPath, '/nv')

Write-Host 'After makeappx'