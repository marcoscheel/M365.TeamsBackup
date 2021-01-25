# Azure CLI must be installed
# https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?tabs=azure-cli
# User must be able to create apps and consent (Global Admin, ...)

$appName = "dev-GKMM-msteamsbackup-app"; #Change for example GKMM to your tenant
$servicePrincipalName = "Microsoft Graph";
$servicePrincipalNameOauth2Permissions = @("Channel.ReadBasic.All", "ChannelMember.Read.All", "ChannelMessage.Read.All", "ChannelSettings.Read.All", "Group.Read.All", "GroupMember.Read.All", "Team.ReadBasic.All", "TeamMember.Read.All", "TeamSettings.Read.All", "TeamsTab.Read.All");

az login --use-device-code --allow-no-subscriptions

$servicePrincipalId = az ad sp list --filter "displayname eq '$servicePrincipalName'" --query '[0].appId' | ConvertFrom-Json

$reqGraph = @{
    resourceAppId = $servicePrincipalId
    resourceAccess = @()
}

(az ad sp show --id $servicePrincipalId --query oauth2Permissions | ConvertFrom-Json) | Where-Object { $_.value -in $servicePrincipalNameOauth2Permissions} | ForEach-Object {
    $permission = $_

    $delPermission = @{
        id = $permission.Id
        type = "Scope"
    }
    $reqGraph.resourceAccess += $delPermission
}

Set-Content ./required_resource_accesses.json -Value ("[" + ($reqGraph | ConvertTo-Json) + "]")
$newapp = az ad app create --display-name $appName --available-to-other-tenants false --native-app true --required-resource-accesses `@required_resource_accesses.json | ConvertFrom-Json
az ad app permission admin-consent --id $newapp.appId

"ClientId: " + $newapp.appId;
"TenantId: " + (az account show | ConvertFrom-Json).tenantId;