# Configuration
$packageDir = '.\Package'
$artifactDir = '.\bin\Package'

# Make sure our CWD is where the script lives
Set-Location $PSScriptRoot

# Fetch the plugin info
$assemblyInfoPath = 'Properties\AssemblyInfo.cs'
$versionPattern = '^\[assembly: AssemblyVersion\("(.*)"\)\]'
$namePattern = '<AssemblyName>(.*)</AssemblyName>'
(Get-Content $assemblyInfoPath) | ForEach-Object {
    if ($_ -match $versionPattern) {
        $assemblyVersion = [version]$matches[1]
    }
}
$csprojPath = Get-ChildItem '.' -filter '*.csproj'
(Get-Content $csprojPath) | ForEach-Object {
    if ($_ -match $namePattern) {
        $modName = $matches[1]
    }
}

# Format the version number for our archive
$modVersion = '{0}.{1}.{2}' -f $assemblyVersion.Major, $assemblyVersion.Minor, $assemblyVersion.Build
if ($assemblyVersion.Revision -ne 0)
{
    $modVersion = '{0}.{1}' -f $modVersion, $assemblyVersion.Revision
}

Write-Host ('Packaging {0} v{1}' -f $modName, $modVersion)

# Create the package structure
$bepInExDir = '{0}\BepInEx' -f $packageDir
$pluginsDir = '{0}\plugins' -f $bepInExDir
$null = mkdir $pluginsDir -ea 0

# Copy required files to the package structure
$artifactPath = ('{0}\{1}.dll' -f $artifactDir, $modName)
Copy-Item $artifactPath -Destination $pluginsDir

# Create the archive
$archivePath = '{0}\{1}-{2}.7z' -f $packageDir, $modName, $modVersion
if (Test-Path $archivePath)
{
    Remove-Item $archivePath
}
7z a $archivePath $bepInExDir

Write-Host ('Mod packaging complete')