param( [string]$workingDirectory = $PSScriptRoot )
Write-Host 'Executing Powershell script patch-rc-version.ps1 with working directory set to: ' $workingDirectory
Set-Location $workingDirectory

$version = $env:APPVEYOR_BUILD_VERSION
Write-Host 'New version is: ' $version

$path = $workingDirectory + "/dnGREP.ContextMenu/Resource.rc"

(Get-Content $path) `
	-Replace '(\d+[.]\d+[.]\d+[.]\d+)', $version `
	-Replace '(\d+,\d+,\d+,\d+)', $version.Replace('.', ',') |
  Out-File $path

Write-Host 'Updated verion in ' $path
