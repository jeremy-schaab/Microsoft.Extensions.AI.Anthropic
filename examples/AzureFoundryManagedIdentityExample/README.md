# Azure Foundry Managed Identity Example

Production-ready example demonstrating **Azure Managed Identity authentication** with Anthropic Foundry for enterprise .NET applications.

## Overview

This example shows how to:
- Use **DefaultAzureCredential** for flexible authentication
- Configure **Managed Identity** for Azure services
- Deploy to **App Service**, **Functions**, **AKS**, and **Container Apps**
- Implement production-ready patterns with logging and error handling
- Securely authenticate without hardcoded API keys

## Authentication Flow

### DefaultAzureCredential Chain

`DefaultAzureCredential` tries these authentication methods in order:

1. **EnvironmentCredential** - Environment variables (CI/CD pipelines)
2. **WorkloadIdentityCredential** - Azure Kubernetes Service workload identity
3. **ManagedIdentityCredential** - Azure Managed Identity (App Service, Functions, VMs, AKS)
4. **AzureCliCredential** - Azure CLI (`az login`) for local development
5. **AzurePowerShellCredential** - Azure PowerShell for local development
6. **AzureDeveloperCliCredential** - Azure Developer CLI
7. **InteractiveBrowserCredential** - Interactive browser (development only)

### Production vs. Development

**Development**: Uses Azure CLI, Visual Studio, or interactive credentials
**Production**: Uses Managed Identity exclusively

## Local Development Setup

### Prerequisites

- .NET 9.0 SDK
- Azure CLI
- Azure subscription with Anthropic Foundry resource

### Step 1: Install Azure CLI

```bash
# Windows (using winget)
winget install Microsoft.AzureCLI

# macOS
brew install azure-cli

# Linux (Ubuntu/Debian)
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

### Step 2: Login to Azure

```bash
# Login with your Azure account
az login

# Verify login
az account show

# Optional: Set specific subscription
az account set --subscription "Your Subscription Name"
```

### Step 3: Configure Application Settings

**Option A: appsettings.Development.json**

```json
{
  "AnthropicFoundry": {
    "ResourceName": "your-foundry-resource-name",
    "ModelId": "claude-3-5-sonnet-20241022"
  }
}
```

**Option B: User Secrets (Recommended for development)**

```bash
cd examples/AzureFoundryManagedIdentityExample

# Initialize user secrets
dotnet user-secrets init

# Set resource name
dotnet user-secrets set "AnthropicFoundry:ResourceName" "your-foundry-resource-name"

# Optional: Set model ID
dotnet user-secrets set "AnthropicFoundry:ModelId" "claude-3-5-sonnet-20241022"
```

**Option C: Environment Variables**

```bash
# Windows PowerShell
$env:AnthropicFoundry__ResourceName = "your-foundry-resource-name"
$env:AnthropicFoundry__ModelId = "claude-3-5-sonnet-20241022"

# Linux/macOS
export AnthropicFoundry__ResourceName="your-foundry-resource-name"
export AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022"
```

### Step 4: Assign RBAC Permissions

Grant your Azure account permissions to use the Anthropic Foundry resource:

```bash
# Get your user principal ID
USER_ID=$(az ad signed-in-user show --query id -o tsv)

# Get Foundry resource ID
FOUNDRY_RESOURCE_ID=$(az resource show \
  --name "your-foundry-resource-name" \
  --resource-type "Microsoft.MachineLearningServices/foundries" \
  --resource-group "your-resource-group" \
  --query id -o tsv)

# Assign "Cognitive Services User" role
az role assignment create \
  --assignee $USER_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_RESOURCE_ID
```

### Step 5: Run the Example

```bash
cd examples/AzureFoundryManagedIdentityExample
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
info: Program[0]
      Model: claude-3-5-sonnet-20241022
info: ChatService[0]
      === Basic Chat Example ===
info: ChatService[0]
      Response: The capital of France is Paris.
info: ChatService[0]
      Usage - Input: 12 tokens, Output: 8 tokens
```

## Azure App Service Deployment

### Prerequisites

- Azure App Service (Windows or Linux)
- System-assigned or user-assigned managed identity enabled

### Step 1: Create App Service

```bash
# Variables
RESOURCE_GROUP="rg-anthropic-example"
APP_NAME="app-anthropic-example"
LOCATION="eastus"
PLAN_NAME="plan-anthropic-example"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service plan (Linux)
az appservice plan create \
  --name $PLAN_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $PLAN_NAME \
  --runtime "DOTNET|9.0"
```

### Step 2: Enable Managed Identity

**Option A: System-Assigned Identity**

```bash
# Enable system-assigned identity
az webapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)
```

**Option B: User-Assigned Identity**

```bash
# Create user-assigned identity
az identity create \
  --name "id-anthropic-app" \
  --resource-group $RESOURCE_GROUP

# Get identity details
IDENTITY_ID=$(az identity show \
  --name "id-anthropic-app" \
  --resource-group $RESOURCE_GROUP \
  --query id -o tsv)

PRINCIPAL_ID=$(az identity show \
  --name "id-anthropic-app" \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

CLIENT_ID=$(az identity show \
  --name "id-anthropic-app" \
  --resource-group $RESOURCE_GROUP \
  --query clientId -o tsv)

# Assign identity to App Service
az webapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --identities $IDENTITY_ID
```

### Step 3: Assign RBAC Permissions

```bash
# Get Foundry resource ID
FOUNDRY_RESOURCE_ID=$(az resource show \
  --name "your-foundry-resource-name" \
  --resource-type "Microsoft.MachineLearningServices/foundries" \
  --resource-group "your-foundry-rg" \
  --query id -o tsv)

# Assign "Cognitive Services User" role to managed identity
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_RESOURCE_ID
```

### Step 4: Configure Application Settings

```bash
# Set Anthropic Foundry configuration
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    AnthropicFoundry__ResourceName="your-foundry-resource-name" \
    AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022"

# Optional: Set user-assigned identity client ID
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    AnthropicFoundry__ManagedIdentityClientId="$CLIENT_ID"
```

### Step 5: Deploy Application

```bash
# Publish application
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to App Service
az webapp deployment source config-zip \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src deploy.zip
```

### Step 6: Verify Deployment

```bash
# View logs
az webapp log tail \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP

# Test the application
curl https://$APP_NAME.azurewebsites.net
```

## Azure Functions Deployment

### Step 1: Create Function App

```bash
# Variables
FUNC_APP_NAME="func-anthropic-example"
STORAGE_ACCOUNT="stanthropic$(openssl rand -hex 4)"

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

# Create Function App (Linux)
az functionapp create \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --storage-account $STORAGE_ACCOUNT \
  --runtime dotnet-isolated \
  --runtime-version 9.0 \
  --functions-version 4 \
  --os-type Linux \
  --consumption-plan-location $LOCATION
```

### Step 2: Enable Managed Identity

```bash
# Enable system-assigned identity
az functionapp identity assign \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get principal ID
FUNC_PRINCIPAL_ID=$(az functionapp identity show \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)
```

### Step 3: Assign RBAC Permissions

```bash
# Assign "Cognitive Services User" role
az role assignment create \
  --assignee $FUNC_PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_RESOURCE_ID
```

### Step 4: Configure Application Settings

```bash
az functionapp config appsettings set \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    AnthropicFoundry__ResourceName="your-foundry-resource-name" \
    AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022"
```

### Step 5: Deploy Function

```bash
# Deploy using func CLI
func azure functionapp publish $FUNC_APP_NAME
```

## Azure Kubernetes Service (AKS) Deployment

### Prerequisites

- AKS cluster with Workload Identity enabled
- Azure AD integration

### Step 1: Create AKS Cluster with Workload Identity

```bash
# Variables
AKS_NAME="aks-anthropic-example"

# Create AKS cluster with OIDC issuer and Workload Identity
az aks create \
  --name $AKS_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --node-count 2 \
  --enable-oidc-issuer \
  --enable-workload-identity \
  --generate-ssh-keys

# Get cluster credentials
az aks get-credentials \
  --name $AKS_NAME \
  --resource-group $RESOURCE_GROUP
```

### Step 2: Create User-Assigned Managed Identity for Workload

```bash
# Create managed identity for workload
az identity create \
  --name "id-anthropic-workload" \
  --resource-group $RESOURCE_GROUP

# Get identity details
WORKLOAD_IDENTITY_CLIENT_ID=$(az identity show \
  --name "id-anthropic-workload" \
  --resource-group $RESOURCE_GROUP \
  --query clientId -o tsv)

WORKLOAD_PRINCIPAL_ID=$(az identity show \
  --name "id-anthropic-workload" \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)
```

### Step 3: Assign RBAC Permissions

```bash
# Assign "Cognitive Services User" role
az role assignment create \
  --assignee $WORKLOAD_PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_RESOURCE_ID
```

### Step 4: Create Federated Identity Credential

```bash
# Get AKS OIDC issuer URL
AKS_OIDC_ISSUER=$(az aks show \
  --name $AKS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query oidcIssuerProfile.issuerUrl -o tsv)

# Create federated identity credential
az identity federated-credential create \
  --name "fc-anthropic-workload" \
  --identity-name "id-anthropic-workload" \
  --resource-group $RESOURCE_GROUP \
  --issuer $AKS_OIDC_ISSUER \
  --subject "system:serviceaccount:default:anthropic-sa" \
  --audience api://AzureADTokenExchange
```

### Step 5: Create Kubernetes Service Account

Create `k8s-manifests/serviceaccount.yaml`:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: anthropic-sa
  namespace: default
  annotations:
    azure.workload.identity/client-id: "<WORKLOAD_IDENTITY_CLIENT_ID>"
```

Apply:

```bash
# Replace client ID
sed "s/<WORKLOAD_IDENTITY_CLIENT_ID>/$WORKLOAD_IDENTITY_CLIENT_ID/g" \
  k8s-manifests/serviceaccount.yaml | kubectl apply -f -
```

### Step 6: Create ConfigMap

```bash
kubectl create configmap anthropic-config \
  --from-literal=AnthropicFoundry__ResourceName="your-foundry-resource-name" \
  --from-literal=AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022"
```

### Step 7: Deploy Application

Create `k8s-manifests/deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: anthropic-app
  namespace: default
spec:
  replicas: 2
  selector:
    matchLabels:
      app: anthropic-app
  template:
    metadata:
      labels:
        app: anthropic-app
        azure.workload.identity/use: "true"
    spec:
      serviceAccountName: anthropic-sa
      containers:
      - name: app
        image: <your-acr>.azurecr.io/anthropic-app:latest
        envFrom:
        - configMapRef:
            name: anthropic-config
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

Apply:

```bash
kubectl apply -f k8s-manifests/deployment.yaml
```

## Azure Container Apps Deployment

### Step 1: Create Container Apps Environment

```bash
# Variables
CONTAINERAPPS_ENVIRONMENT="env-anthropic"
CONTAINERAPPS_APP="app-anthropic"

# Install Container Apps extension
az extension add --name containerapp --upgrade

# Create Container Apps environment
az containerapp env create \
  --name $CONTAINERAPPS_ENVIRONMENT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

### Step 2: Create User-Assigned Identity

```bash
# Create identity
az identity create \
  --name "id-anthropic-containerapp" \
  --resource-group $RESOURCE_GROUP

# Get details
CONTAINERAPP_IDENTITY_ID=$(az identity show \
  --name "id-anthropic-containerapp" \
  --resource-group $RESOURCE_GROUP \
  --query id -o tsv)

CONTAINERAPP_PRINCIPAL_ID=$(az identity show \
  --name "id-anthropic-containerapp" \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

CONTAINERAPP_CLIENT_ID=$(az identity show \
  --name "id-anthropic-containerapp" \
  --resource-group $RESOURCE_GROUP \
  --query clientId -o tsv)
```

### Step 3: Assign RBAC Permissions

```bash
az role assignment create \
  --assignee $CONTAINERAPP_PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_RESOURCE_ID
```

### Step 4: Create Container App

```bash
az containerapp create \
  --name $CONTAINERAPPS_APP \
  --resource-group $RESOURCE_GROUP \
  --environment $CONTAINERAPPS_ENVIRONMENT \
  --image mcr.microsoft.com/dotnet/samples:aspnetapp \
  --target-port 8080 \
  --ingress external \
  --user-assigned $CONTAINERAPP_IDENTITY_ID \
  --env-vars \
    AnthropicFoundry__ResourceName="your-foundry-resource-name" \
    AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022" \
    AnthropicFoundry__ManagedIdentityClientId="$CONTAINERAPP_CLIENT_ID" \
  --cpu 0.5 \
  --memory 1.0Gi \
  --min-replicas 1 \
  --max-replicas 5
```

## RBAC Role Assignments

### Required Azure Roles

| Role | Purpose | Scope |
|------|---------|-------|
| **Cognitive Services User** | Use Anthropic Foundry API | Foundry resource |
| **Cognitive Services OpenAI User** | Alternative role (if available) | Foundry resource |

### Assign Roles via Azure Portal

1. Navigate to your Anthropic Foundry resource
2. Click **Access control (IAM)**
3. Click **Add role assignment**
4. Select role: **Cognitive Services User**
5. Click **Next**
6. Select **Managed identity**
7. Click **Select members**
8. Choose your managed identity
9. Click **Review + assign**

### Verify Role Assignment

```bash
# List role assignments for the Foundry resource
az role assignment list \
  --scope $FOUNDRY_RESOURCE_ID \
  --output table
```

## Configuration Reference

### appsettings.json

```json
{
  "AnthropicFoundry": {
    "ResourceName": "your-foundry-resource-name",
    "ModelId": "claude-3-5-sonnet-20241022",
    "ManagedIdentityClientId": null,
    "TenantId": null
  }
}
```

### Configuration Sources Priority (highest to lowest)

1. Command-line arguments
2. Environment variables
3. User secrets (development only)
4. appsettings.{Environment}.json
5. appsettings.json

### Environment Variable Naming

```bash
# Double underscore (__) replaces colon (:) in JSON path
AnthropicFoundry__ResourceName="your-resource"
AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022"
AnthropicFoundry__ManagedIdentityClientId="client-id-guid"
```

## Troubleshooting

### Issue: "DefaultAzureCredential failed to retrieve a token"

**Symptoms**: Authentication fails with no valid credentials found

**Solutions**:

1. **Local Development**: Ensure `az login` completed successfully
   ```bash
   az login
   az account show
   ```

2. **Azure Service**: Verify managed identity is enabled
   ```bash
   # For App Service
   az webapp identity show --name <app-name> --resource-group <rg>

   # For Function App
   az functionapp identity show --name <func-name> --resource-group <rg>
   ```

3. **Check RBAC**: Verify role assignment
   ```bash
   az role assignment list --scope $FOUNDRY_RESOURCE_ID
   ```

4. **Enable verbose logging**:
   ```json
   "Logging": {
     "LogLevel": {
       "Azure.Identity": "Debug"
     }
   }
   ```

### Issue: "Managed identity is not enabled"

**Solution**: Enable managed identity on your Azure service

```bash
# App Service
az webapp identity assign --name <app-name> --resource-group <rg>

# Function App
az functionapp identity assign --name <func-name> --resource-group <rg>
```

### Issue: "User does not have permission to use resource"

**Solution**: Assign "Cognitive Services User" role

```bash
PRINCIPAL_ID="<managed-identity-principal-id>"
FOUNDRY_RESOURCE_ID="<foundry-resource-id>"

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_RESOURCE_ID
```

### Issue: AKS Workload Identity not working

**Checklist**:

1. OIDC issuer enabled on cluster
   ```bash
   az aks show --name <aks-name> --resource-group <rg> --query oidcIssuerProfile.issuerUrl
   ```

2. Workload identity enabled
   ```bash
   az aks show --name <aks-name> --resource-group <rg> --query securityProfile.workloadIdentity.enabled
   ```

3. Federated credential created with correct subject
   ```bash
   az identity federated-credential list \
     --identity-name <identity-name> \
     --resource-group <rg>
   ```

4. Service account has correct annotation
   ```bash
   kubectl get serviceaccount anthropic-sa -o yaml
   ```

5. Pod has correct label
   ```yaml
   labels:
     azure.workload.identity/use: "true"
   ```

### Issue: "Configuration 'AnthropicFoundry:ResourceName' not found"

**Solution**: Set configuration in one of these ways:

```bash
# Environment variable
export AnthropicFoundry__ResourceName="your-resource"

# User secrets (development)
dotnet user-secrets set "AnthropicFoundry:ResourceName" "your-resource"

# appsettings.json
{
  "AnthropicFoundry": {
    "ResourceName": "your-resource"
  }
}
```

## Security Best Practices

1. **Never hardcode API keys** - Use managed identity for production
2. **Use user-assigned identities** for better isolation and reusability
3. **Apply least-privilege RBAC** - Only assign necessary roles
4. **Rotate credentials** - Managed identities handle this automatically
5. **Enable diagnostic logging** - Monitor authentication events
6. **Use Azure Key Vault** for additional secrets (if needed)
7. **Implement network isolation** - Use private endpoints where possible
8. **Enable Azure Policy** - Enforce managed identity requirements

## Performance Considerations

- **Connection pooling**: Reuse `IChatClient` instances (registered as singleton)
- **Async operations**: Always use async methods
- **Cancellation tokens**: Support cancellation for long-running operations
- **Retry policies**: DefaultAzureCredential includes built-in retry logic
- **Token caching**: Azure.Identity automatically caches tokens

## Related Documentation

- [Azure Managed Identity Overview](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
- [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)
- [Azure RBAC](https://learn.microsoft.com/azure/role-based-access-control/overview)
- [AKS Workload Identity](https://learn.microsoft.com/azure/aks/workload-identity-overview)
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)

## Support

For issues or questions:
- File an issue at: https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic/issues
- Review examples at: `examples/`
- Check logs with `Azure.Identity` logging enabled
