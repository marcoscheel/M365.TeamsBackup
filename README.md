# M365.TeamsBackup

Check out my blog posts regarding the tool:
- [Microsoft Teams backup your channel messages with Microsoft Graph](https://marcoscheel.de/post/2020/12/20201130-m365teamsbackup/)
- [Create your Azure AD application via script - M365.TeamsBackup](https://marcoscheel.de/post/2021/01/20210124-m365teamsbackup-aadapp/)

## Usage
Add the account to all teams to backup. Ensure he has access to all private channels or private channels will not be backedup

Run executable with parameter for the environment "--environment Production"

## Configuration

### Azure AD App registration

I tested the setup as a Global Administrator and you should also use a Global Admin to setup.

You can setup the application via the includes scripts:
- Using Azure AD PowerShell module (manuel admin consent)
  - .\deploy\create-aadapp.ps1
  - Change the name of the app to your liking ($appName)
- Using Azure CLI (including admin consent)
  - .\deploy\create-aadapp-cli.ps1
  - Change the name of the app to your liking ($appName)

Or for a manuel setup through the Azure portal:
- Name: e.g. dev-GKMM-msteamsbackup-app
- Supported account types: My organization only
- Authentication
  - Add mobile and desktop
  - Advanced Settings
    - Allow public client flows: Yes
- API Permission
  - MS Graph
    - Delegate
      - Channel.ReadBasic.All
      - ChannelMember.Read.All
      - ChannelMessage.Read.All
      - ChannelSettings.Read.All
      - Group.Read.All
      - GroupMember.Read.All
      - Team.ReadBasic.All
      - TeamMember.Read.All
      - TeamSettings.Read.All
      - TeamsTab.Read.All
  - Grant admion consent for ORG

### BackupToHtmlConsole
appsettings(.Development|.Production).json
-  M365
  - AzureAd
    - Instance: Should be https://login.microsoftonline.com in most cases
    - ClientId: Azure AD App Id from your tenant. The app uses the device code flow only at the moment
    - TenantId: Your tenant ID required for the audience
    - Scope (string array): Most of the time https://graph.microsoft.com/.default is good enough 
  - Backup
    - Path: Output directory for the JSON files
    - JsonWriteIndented: Use pretty (easy to read) JSON with formating. Setting to "false" will save some space.
    - TeamId: Optional. If filled only this team will be used for backup
### BackupConsole
appsettings.(Development|Production).json
- M365
  - Html
    - SourcePath: Location of the JSON files
    - TargetPath: Location the HTMLs will be saved to
    - TemplateFile: Html templated loaded for every html file. Allows styling (css) of the content
    - UseInlineImages: Create a single HTML file and all images are BASE64 encoded. Note: Html will be very large!
    - CreateSingleHtmlForMessage: In addition to the channel.html file every thread will also be saved as a HTML message. Could be a benefit for SharePoint search, display in SPO and smaller files in general.

