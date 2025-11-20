# Quick Start - Azure Foundry Managed Identity Example

Get up and running in 5 minutes with Azure Managed Identity authentication.

## Prerequisites

- .NET 9.0 SDK
- Azure subscription
- Azure Anthropic Foundry resource
- Azure CLI (for local development)

## Local Development (Azure CLI)

### Step 1: Login to Azure

```bash
az login
```

### Step 2: Set Configuration

**Option A: User Secrets (Recommended)**

```bash
cd examples/AzureFoundryManagedIdentityExample

dotnet user-secrets set "AnthropicFoundry:ResourceName" "your-foundry-resource-name"
dotnet user-secrets set "AnthropicFoundry:ModelId" "claude-3-5-sonnet-20241022"
```

**Option B: Environment Variables**

Windows PowerShell:
```powershell
$env:AnthropicFoundry__ResourceName = "your-foundry-resource-name"
$env:AnthropicFoundry__ModelId = "claude-3-5-sonnet-20241022"
```

Linux/macOS:
```bash
export AnthropicFoundry__ResourceName="your-foundry-resource-name"
export AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022"
```

### Step 3: Grant Yourself Access

```bash
# Get your user ID
USER_ID=$(az ad signed-in-user show --query id -o tsv)

# Get Foundry resource ID (replace with your values)
FOUNDRY_ID=$(az resource show \
  --name "your-foundry-resource-name" \
  --resource-type "Microsoft.MachineLearningServices/foundries" \
  --resource-group "your-resource-group" \
  --query id -o tsv)

# Assign role
az role assignment create \
  --assignee $USER_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_ID
```

### Step 4: Run

```bash
dotnet run
```

Expected output:
```
info: Program[0]
      Starting Azure Foundry Managed Identity Example
info: Program[0]
      Environment: Development
info: Program[0]
      Resource: your-foundry-resource-name
info: ChatService[0]
      === Basic Chat Example ===
info: ChatService[0]
      Response: The capital of France is Paris.
```

## Azure Deployment (5-Minute App Service)

```bash
# Variables
RG="rg-anthropic-demo"
APP="app-anthropic-demo-$(openssl rand -hex 4)"
LOCATION="eastus"
FOUNDRY_NAME="your-foundry-resource"
FOUNDRY_RG="your-foundry-rg"

# Create and deploy
az group create --name $RG --location $LOCATION

az webapp up \
  --name $APP \
  --resource-group $RG \
  --runtime "DOTNET:9.0" \
  --sku B1

# Enable managed identity
az webapp identity assign --name $APP --resource-group $RG

# Get principal ID
PRINCIPAL_ID=$(az webapp identity show --name $APP --resource-group $RG --query principalId -o tsv)

# Get Foundry ID
FOUNDRY_ID=$(az resource show \
  --name $FOUNDRY_NAME \
  --resource-type "Microsoft.MachineLearningServices/foundries" \
  --resource-group $FOUNDRY_RG \
  --query id -o tsv)

# Grant access
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_ID

# Configure
az webapp config appsettings set \
  --name $APP \
  --resource-group $RG \
  --settings \
    AnthropicFoundry__ResourceName=$FOUNDRY_NAME \
    AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022"

echo "Deployed to: https://$APP.azurewebsites.net"
```

## Troubleshooting

### "DefaultAzureCredential failed to retrieve a token"

**Local Development:**
- Run `az login` and verify with `az account show`
- Ensure you have "Cognitive Services User" role on the Foundry resource

**Azure Deployment:**
- Verify managed identity is enabled: `az webapp identity show --name <app> --resource-group <rg>`
- Check RBAC role assignment: `az role assignment list --scope <foundry-resource-id>`

### "Configuration 'AnthropicFoundry:ResourceName' not found"

- Verify user secrets: `dotnet user-secrets list`
- Or check environment variables: `echo $env:AnthropicFoundry__ResourceName` (PowerShell) or `echo $AnthropicFoundry__ResourceName` (bash)

### "User does not have permission"

Grant yourself the role:
```bash
az role assignment create \
  --assignee <your-user-id-or-principal-id> \
  --role "Cognitive Services User" \
  --scope <foundry-resource-id>
```

Wait 2-5 minutes for RBAC propagation.

## Next Steps

- See [README.md](README.md) for detailed authentication flow
- See [DEPLOYMENT.md](DEPLOYMENT.md) for production deployment guides
- Explore different Azure services (Functions, AKS, Container Apps)
- Add middleware (logging, telemetry, caching)
- Implement custom retry policies

## Configuration Options

| Setting | Required | Default | Description |
|---------|----------|---------|-------------|
| `AnthropicFoundry:ResourceName` | Yes | - | Azure Foundry resource name |
| `AnthropicFoundry:ModelId` | No | claude-3-5-sonnet-20241022 | Claude model ID |
| `AnthropicFoundry:ManagedIdentityClientId` | No | - | User-assigned identity client ID (system-assigned if null) |
| `AnthropicFoundry:TenantId` | No | - | Azure AD tenant ID |

## Available Models

- `claude-3-5-sonnet-20241022` - Latest Sonnet (recommended)
- `claude-3-opus-20240229` - Most capable
- `claude-3-haiku-20240307` - Fastest, most cost-effective
- `claude-opus-4-20250514` - Opus 4 (if available in your region)

## Support

- GitHub Issues: https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic/issues
- Documentation: [README.md](README.md)
- Examples: `examples/` directory
