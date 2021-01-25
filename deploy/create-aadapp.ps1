# Requires AzureADPreview Module;
# Install-Module AzureADPreview
# User must be able to create apps and consent (Global Admin, ...)

$appName = "dev-GKMM-msteamsbackup-app"; #Change for example GKMM to your tenant
$servicePrincipalName = "Microsoft Graph";
$servicePrincipalNameOauth2Permissions = @("Channel.ReadBasic.All", "ChannelMember.Read.All", "ChannelMessage.Read.All", "ChannelSettings.Read.All", "Group.Read.All", "GroupMember.Read.All", "Team.ReadBasic.All", "TeamMember.Read.All", "TeamSettings.Read.All", "TeamsTab.Read.All");

# login
Connect-AzureAD;

# Get MS Graph
$servicePrincipal = Get-AzureADServicePrincipal -All $true | Where-Object { $_.DisplayName -eq $servicePrincipalName };

# Thanks http://blog.octavie.nl/index.php/2017/09/19/create-azure-ad-app-registration-with-powershell-part-2
$reqGraph = New-Object -TypeName "Microsoft.Open.AzureAD.Model.RequiredResourceAccess";
$reqGraph.ResourceAppId = $servicePrincipal.AppId;

$servicePrincipal.Oauth2Permissions | Where-Object { $_.Value -in $servicePrincipalNameOauth2Permissions} | ForEach-Object {
    $permission = $_
    $delPermission = New-Object -TypeName "Microsoft.Open.AzureAD.Model.ResourceAccess" -ArgumentList $permission.Id,"Scope" #delegate permission (oauth) are always "Scope"
    $reqGraph.ResourceAccess += $delPermission
}

New-AzureADApplication -DisplayName $appName -AvailableToOtherTenants:$false -PublicClient:$true -RequiredResourceAccess $reqGraph;
$newapp = Get-AzureADApplication -SearchString $appName;
"ClientId: " + $newapp.AppId;
"TenantId: " + (Get-AzureADTenantDetail).ObjectId;

"TODO: Consent in portal";
"Check AAD app: https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/CallAnAPI/appId/" + $newapp.AppId + "/objectId/" + $newapp.ObjectId + "/isMSAApp/";