function ParseVersion(
    #Path to AssemblyVersionCommon.cs
    $AssemblyVersionCommonCSPath
    )
{
    # Parse the version number out of the AssemblyVersionCommon.cs file 
    $matches = Select-String -Path $AssemblyVersionCommonCSPath -Pattern '\[assembly: AssemblyVersion\("(\w+)\.(\w+)\.(\w+)\.(\w+)"\)\]'
    
    return New-Object PSObject -Property @{
        'MajorVersion' = $matches.Matches.Groups[1].Value;
        'MinorVersion' = $matches.Matches.Groups[2].Value;
        'BuildVersion' = $matches.Matches.Groups[3].Value;
        'Revision'     = $matches.Matches.Groups[4].Value}
}
