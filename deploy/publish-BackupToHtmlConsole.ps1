#Build release
$corePathToProject = "..\src\BackupToHtmlConsole";
$configNames = @("Release");

foreach($configName in $configNames){
    $configName;
    $publishLocation = ".\$configName-output";
    $publishLocationApp = $publishLocation + "\M365.TeamsBackup.BackupToHtmlConsole.exe";
    

    dotnet publish $corePathToProject --configuration $configName --output $publishLocation;

    Remove-Item "$publishLocation/appsettings.Development.json";
    Remove-Item "$publishLocation/appsettings.Production.json";
    
    if (Test-Path $publishLocationApp){
        
        if ((Test-Path $configName) -eq $false){
            mkdir $configName;
        }

        $version2publish = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Get-Location).ToString() + $publishLocationApp).FileVersion.ToString().Replace(".", "-");
    
        $thisZipVersion = ".\" + $configName + "\M365.TeamsBackup.BackupToHtmlConsole-V-" + $version2publish + "-" + $configName + ".zip";

        Compress-Archive ($publishLocation + "\*") $thisZipVersion -CompressionLevel Fastest;
        Remove-Item $publishLocation -Recurse:$true
    }
}