# M365.TeamsBackup

## Usage
Add the account to all teams to backup. Ensure he has access to all private channels or private channels will not be backedup

Run executable with parameter for the environment "--environment Production"

## Configuration

### Azure AD App registration

Create a teams app as a global admin in your tenant.
- Name: e.g. dev-GKMM-msteamsbackup-app
- Supported account types: My organization only
- Authentication
  - Add mobile and desktop
  - Copy redirect URI msal...
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
appsettings.(Development|Production).json
-  M365
  - AzureAd
    - Instance: Should be https://login.microsoftonline.com in most cases
    - ClientId: Azure AD App Id from your tenant. The app uses the device code flow only at the moment
    - TenantId: Your tenant ID required for the audience
    - ReplyUri: Reply URI from the App app registration site for the App
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

