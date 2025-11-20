# Authentication Guide - Microsoft.Extensions.AI.Anthropic

**Version**: 0.3.1-preview
**Last Updated**: 2025-01-19
**Audience**: .NET developers deploying production AI applications

This guide covers all authentication methods supported by Microsoft.Extensions.AI.Anthropic, with emphasis on production-ready security practices for Azure Foundry deployments.

## Table of Contents

- [Overview](#overview)
- [Azure Foundry Authentication](#azure-foundry-authentication)
  - [1. Azure Identity (Recommended)](#1-azure-identity-recommended)
  - [2. API Key](#2-api-key)
  - [3. Bearer Token](#3-bearer-token)
- [Standard API Authentication](#standard-api-authentication)
- [Environment Variables](#environment-variables)
- [Production Deployment Patterns](#production-deployment-patterns)
- [Security Best Practices](#security-best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

Microsoft.Extensions.AI.Anthropic supports **two API endpoints**:

| API Type | Endpoint | Primary Use Case | Authentication |
|----------|----------|------------------|----------------|
| **Azure Foundry** | `*.ai.azure.com` | Production, Enterprise | API Key, Azure Identity, Bearer Token |
| **Standard API** | `api.anthropic.com` | Development, Prototyping | API Key |

**Recommendation**: Use **Azure Foundry with Azure Identity** (Managed Identity) for all production deployments.

### Why Azure Foundry?

1. **No Hardcoded Secrets**: Managed Identity eliminates API keys in code
2. **Azure RBAC**: Fine-grained access control
3. **Compliance**: Azure compliance certifications (SOC 2, HIPAA, etc.)
4. **Private Endpoints**: Network isolation
5. **Enterprise Support**: Azure SLA and support contracts

## Azure Foundry Authentication

Azure Foundry supports three authentication methods, listed in order of preference for production:

### 1. Azure Identity (Recommended)

Uses `DefaultAzureCredential` which automatically tries multiple authentication methods in order.

#### How It Works

The `DefaultAzureCredential` chain tries these sources in order:

```
1. Environment Variables (EnvironmentCredential)
   ↓
2. Managed Identity (ManagedIdentityCredential)
   ↓
3. Visual Studio (VisualStudioCredential)
   ↓
4. Visual Studio Code (VisualStudioCodeCredential)
   ↓
5. Azure CLI (AzureCliCredential)
   ↓
6. Azure PowerShell (AzurePowerShellCredential)
   ↓
7. Interactive Browser (InteractiveBrowserCredential)
```

#### Implementation

**Step 1: Install Azure.Identity**

```bash
dotnet add package Azure.Identity
```

**Step 2: Create Credentials**

```csharp
using Anthropic.Foundry;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;

var credentials = new AnthropicFoundryIdentityTokenCredentials(
    tokenCredential: new DefaultAzureCredential(),
    resourceName: "my-anthropic-resource");

var foundryClient = new AnthropicFoundryClient(credentials);
IChatClient chatClient = new AnthropicChatClient(foundryClient, "claude-sonnet-4-5");
```

**Step 3: Use in DI**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAnthropicFoundryChatClient(
    credentials: new AnthropicFoundryIdentityTokenCredentials(
        tokenCredential: new DefaultAzureCredential(),
        resourceName: "my-anthropic-resource"),
    modelId: "claude-sonnet-4-5");

var host = builder.Build();
var chatClient = host.Services.GetRequiredService<IChatClient>();
```

#### Local Development Setup

**Option 1: Azure CLI** (Recommended)

```bash
# Login with your Azure account
az login

# Verify login
az account show

# Your app will now use Azure CLI credentials
```

**Option 2: Environment Variables**

```bash
# Set environment variables for service principal
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
export AZURE_TENANT_ID="your-tenant-id"
```

**Option 3: Visual Studio**

1. Tools → Options → Azure Service Authentication
2. Sign in with your Azure account
3. Credentials automatically used

#### Production Deployment

**Azure App Service / Functions**:

```bash
# Enable system-assigned managed identity
az webapp identity assign \
  --name my-app \
  --resource-group my-rg

# Get the principal ID (identity)
principalId=$(az webapp identity show \
  --name my-app \
  --resource-group my-rg \
  --query principalId -o tsv)

# Grant access to Anthropic Foundry resource
az role assignment create \
  --role "Cognitive Services User" \
  --assignee $principalId \
  --scope /subscriptions/{subscription-id}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource-name}
```

**Azure Container Apps**:

```bash
# Enable system-assigned managed identity
az containerapp identity assign \
  --name my-app \
  --resource-group my-rg

# Grant RBAC permissions (same as above)
```

**Azure Kubernetes Service (AKS)**:

Use **Workload Identity** (recommended) or **Pod Identity**:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: anthropic-app
  annotations:
    azure.workload.identity/client-id: YOUR_CLIENT_ID
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: anthropic-app
spec:
  template:
    metadata:
      labels:
        azure.workload.identity/use: "true"
    spec:
      serviceAccountName: anthropic-app
      containers:
      - name: app
        image: myapp:latest
        env:
        - name: ANTHROPIC_FOUNDRY_RESOURCE
          value: "my-anthropic-resource"
```

#### Custom TokenCredential

Use a specific credential type instead of `DefaultAzureCredential`:

```csharp
using Azure.Identity;

// Managed Identity (system-assigned)
var credential = new ManagedIdentityCredential();

// Managed Identity (user-assigned)
var credential = new ManagedIdentityCredential(clientId: "your-client-id");

// Service Principal
var credential = new ClientSecretCredential(
    tenantId: "your-tenant-id",
    clientId: "your-client-id",
    clientSecret: "your-client-secret");

// Azure CLI
var credential = new AzureCliCredential();

// Chain multiple credentials
var credential = new ChainedTokenCredential(
    new ManagedIdentityCredential(),
    new AzureCliCredential());

var credentials = new AnthropicFoundryIdentityTokenCredentials(
    tokenCredential: credential,
    resourceName: "my-anthropic-resource");
```

### 2. API Key

Uses a static API key for authentication. Suitable for development and testing.

#### Implementation

**Direct Instantiation**:

```csharp
using Anthropic.Foundry;
using Microsoft.Extensions.AI.Anthropic;

var credentials = new AnthropicFoundryApiKeyCredentials(
    apiKey: "your-api-key",
    resourceName: "my-anthropic-resource");

var foundryClient = new AnthropicFoundryClient(credentials);
IChatClient chatClient = new AnthropicChatClient(foundryClient, "claude-sonnet-4-5");
```

**With Dependency Injection**:

```csharp
builder.Services.AddAnthropicFoundryChatClient(
    resourceName: "my-anthropic-resource",
    apiKey: "your-api-key",
    modelId: "claude-sonnet-4-5");
```

**From Configuration**:

```csharp
// appsettings.json
{
  "Azure": {
    "Anthropic": {
      "ResourceName": "my-anthropic-resource",
      "ApiKey": "your-api-key"
    }
  }
}

// Program.cs
builder.Services.AddAnthropicFoundryChatClient(
    resourceName: builder.Configuration["Azure:Anthropic:ResourceName"],
    apiKey: builder.Configuration["Azure:Anthropic:ApiKey"],
    modelId: "claude-sonnet-4-5");
```

**From Environment Variables**:

```bash
export ANTHROPIC_FOUNDRY_RESOURCE="my-anthropic-resource"
export ANTHROPIC_FOUNDRY_API_KEY="your-api-key"
```

```csharp
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");
```

#### Getting API Keys

1. **Azure Portal**: Navigate to your Anthropic Foundry resource
2. **Keys and Endpoint**: Find in the left menu
3. **Copy Key**: Use either Key 1 or Key 2

**Security Notes**:
- Rotate keys regularly (every 90 days)
- Use Azure Key Vault in production (see [Best Practices](#security-best-practices))
- Never commit keys to source control

### 3. Bearer Token

Uses a bearer token for authentication. Useful for custom token providers or short-lived tokens.

#### Implementation

```csharp
using Anthropic.Foundry;

var credentials = new AnthropicFoundryBearerTokenCredentials(
    bearerToken: "your-bearer-token",
    resourceName: "my-anthropic-resource");

var foundryClient = new AnthropicFoundryClient(credentials);
IChatClient chatClient = new AnthropicChatClient(foundryClient, "claude-sonnet-4-5");
```

**With Token Refresh**:

```csharp
public class TokenProvider
{
    public async Task<string> GetTokenAsync()
    {
        // Custom logic to obtain/refresh token
        return await FetchTokenFromAuthServerAsync();
    }
}

// Usage
var tokenProvider = new TokenProvider();
var token = await tokenProvider.GetTokenAsync();

var credentials = new AnthropicFoundryBearerTokenCredentials(
    bearerToken: token,
    resourceName: "my-anthropic-resource");
```

**Note**: Bearer tokens typically have expiration times. Implement refresh logic to avoid authentication failures.

## Standard API Authentication

The standard Anthropic API uses a simple API key authentication.

### Implementation

**Direct Instantiation**:

```csharp
using Anthropic;
using Microsoft.Extensions.AI.Anthropic;

var anthropicClient = new AnthropicClient(new ClientOptions
{
    APIKey = "sk-ant-api03-..."
});

IChatClient chatClient = new AnthropicChatClient(
    anthropicClient,
    modelId: "claude-sonnet-4-5");
```

**With Dependency Injection**:

```csharp
builder.Services.AddAnthropicChatClient(
    apiKey: "sk-ant-api03-...",
    modelId: "claude-sonnet-4-5");
```

**From Environment Variables**:

```bash
export ANTHROPIC_API_KEY="sk-ant-api03-..."
```

```csharp
var anthropicClient = new AnthropicClient(new ClientOptions
{
    APIKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
});
```

**From Configuration**:

```csharp
// appsettings.json
{
  "Anthropic": {
    "ApiKey": "sk-ant-api03-..."
  }
}

// Program.cs
builder.Services.AddAnthropicChatClient(
    apiKey: builder.Configuration["Anthropic:ApiKey"],
    modelId: "claude-sonnet-4-5");
```

### Getting API Keys

1. Visit https://console.anthropic.com/
2. Navigate to **API Keys**
3. Click **Create Key**
4. Copy and securely store the key

**Security Notes**:
- API keys start with `sk-ant-api03-`
- Never commit to source control
- Use environment variables or secret management
- Monitor usage to detect unauthorized access

## Environment Variables

### Azure Foundry

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `ANTHROPIC_FOUNDRY_RESOURCE` | Yes | Azure resource name | `my-anthropic-resource` |
| `ANTHROPIC_FOUNDRY_API_KEY` | No* | API key | `your-api-key` |

*Required if not using Azure Identity

**Usage**:

```bash
# Windows (Command Prompt)
set ANTHROPIC_FOUNDRY_RESOURCE=my-resource
set ANTHROPIC_FOUNDRY_API_KEY=your-api-key

# Windows (PowerShell)
$env:ANTHROPIC_FOUNDRY_RESOURCE="my-resource"
$env:ANTHROPIC_FOUNDRY_API_KEY="your-api-key"

# Linux/macOS
export ANTHROPIC_FOUNDRY_RESOURCE="my-resource"
export ANTHROPIC_FOUNDRY_API_KEY="your-api-key"
```

### Standard API

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `ANTHROPIC_API_KEY` | Yes | Anthropic API key | `sk-ant-api03-...` |

**Usage**:

```bash
# Windows
set ANTHROPIC_API_KEY=sk-ant-api03-...

# Linux/macOS
export ANTHROPIC_API_KEY=sk-ant-api03-...
```

### Azure Identity (Optional)

| Variable | Required | Description |
|----------|----------|-------------|
| `AZURE_CLIENT_ID` | No | Service principal client ID |
| `AZURE_CLIENT_SECRET` | No | Service principal secret |
| `AZURE_TENANT_ID` | No | Azure AD tenant ID |

These are only needed if using **EnvironmentCredential** (first in `DefaultAzureCredential` chain).

## Production Deployment Patterns

### Pattern 1: Azure App Service with Managed Identity

**Best for**: Web applications, APIs

```bash
# 1. Create App Service
az webapp create \
  --name my-app \
  --resource-group my-rg \
  --plan my-plan \
  --runtime "DOTNET|9.0"

# 2. Enable managed identity
az webapp identity assign \
  --name my-app \
  --resource-group my-rg

# 3. Get principal ID
principalId=$(az webapp identity show \
  --name my-app \
  --resource-group my-rg \
  --query principalId -o tsv)

# 4. Grant RBAC permissions
az role assignment create \
  --role "Cognitive Services User" \
  --assignee $principalId \
  --scope /subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource}

# 5. Set environment variable
az webapp config appsettings set \
  --name my-app \
  --resource-group my-rg \
  --settings ANTHROPIC_FOUNDRY_RESOURCE=my-resource
```

**Code** (no changes needed):

```csharp
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");
```

### Pattern 2: Azure Functions with Managed Identity

**Best for**: Event-driven processing, scheduled jobs

```bash
# 1. Create Function App
az functionapp create \
  --name my-function \
  --resource-group my-rg \
  --storage-account mystorage \
  --runtime dotnet-isolated \
  --runtime-version 9 \
  --functions-version 4

# 2. Enable managed identity
az functionapp identity assign \
  --name my-function \
  --resource-group my-rg

# 3. Grant permissions (same as above)

# 4. Set app setting
az functionapp config appsettings set \
  --name my-function \
  --resource-group my-rg \
  --settings ANTHROPIC_FOUNDRY_RESOURCE=my-resource
```

**Function Code**:

```csharp
[Function("ProcessMessage")]
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
    [FromServices] IChatClient chatClient)
{
    var userMessage = await req.ReadAsStringAsync();
    var response = await chatClient.GetResponseAsync(userMessage);
    return new OkObjectResult(response.Text);
}
```

### Pattern 3: Azure Container Apps with Managed Identity

**Best for**: Containerized applications, microservices

```bash
# 1. Create container app
az containerapp create \
  --name my-app \
  --resource-group my-rg \
  --environment my-env \
  --image myregistry.azurecr.io/myapp:latest \
  --cpu 0.5 --memory 1.0Gi

# 2. Enable managed identity
az containerapp identity assign \
  --name my-app \
  --resource-group my-rg \
  --system-assigned

# 3. Grant permissions (same as above)

# 4. Set environment variable
az containerapp update \
  --name my-app \
  --resource-group my-rg \
  --set-env-vars ANTHROPIC_FOUNDRY_RESOURCE=my-resource
```

### Pattern 4: Azure Kubernetes Service (AKS) with Workload Identity

**Best for**: Kubernetes workloads

```bash
# 1. Create user-assigned managed identity
az identity create \
  --name my-app-identity \
  --resource-group my-rg

# 2. Get identity client ID
clientId=$(az identity show \
  --name my-app-identity \
  --resource-group my-rg \
  --query clientId -o tsv)

# 3. Grant permissions
az role assignment create \
  --role "Cognitive Services User" \
  --assignee $clientId \
  --scope /subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource}

# 4. Configure workload identity federation
az identity federated-credential create \
  --name my-app-fedcred \
  --identity-name my-app-identity \
  --resource-group my-rg \
  --issuer "https://oidc.prod-aks.azure.com/{tenant-id}/" \
  --subject "system:serviceaccount:default:my-app"
```

**Kubernetes Deployment**:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: my-app
  annotations:
    azure.workload.identity/client-id: YOUR_CLIENT_ID
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-app
spec:
  template:
    metadata:
      labels:
        azure.workload.identity/use: "true"
    spec:
      serviceAccountName: my-app
      containers:
      - name: app
        image: myregistry.azurecr.io/myapp:latest
        env:
        - name: ANTHROPIC_FOUNDRY_RESOURCE
          value: "my-resource"
```

### Pattern 5: Azure Key Vault Integration

**Best for**: Storing API keys securely

```bash
# 1. Create Key Vault
az keyvault create \
  --name my-keyvault \
  --resource-group my-rg

# 2. Store API key
az keyvault secret set \
  --vault-name my-keyvault \
  --name anthropic-api-key \
  --value "your-api-key"

# 3. Grant App Service access
principalId=$(az webapp identity show \
  --name my-app \
  --resource-group my-rg \
  --query principalId -o tsv)

az keyvault set-policy \
  --name my-keyvault \
  --object-id $principalId \
  --secret-permissions get

# 4. Reference in App Settings
az webapp config appsettings set \
  --name my-app \
  --resource-group my-rg \
  --settings \
    ANTHROPIC_FOUNDRY_RESOURCE=my-resource \
    ANTHROPIC_FOUNDRY_API_KEY="@Microsoft.KeyVault(SecretUri=https://my-keyvault.vault.azure.net/secrets/anthropic-api-key/)"
```

**Code** (no changes needed):

```csharp
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");
```

## Security Best Practices

### 1. Never Hardcode Secrets

**BAD**:
```csharp
var apiKey = "sk-ant-api03-abcdef12345";  // NEVER DO THIS
```

**GOOD**:
```csharp
var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
var apiKey = configuration["Anthropic:ApiKey"];  // From secure config
```

### 2. Use Azure Identity in Production

**Preference Order**:
1. **Managed Identity** (Azure resources)
2. **Azure Key Vault** (API keys)
3. **Service Principal** (CI/CD)
4. **API Keys** (development only)

### 3. Implement Least Privilege

Grant only the minimum required permissions:

```bash
# Grant read-only access to Anthropic resource
az role assignment create \
  --role "Cognitive Services User" \  # NOT "Cognitive Services Contributor"
  --assignee $principalId \
  --scope /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource}
```

### 4. Rotate Keys Regularly

**API Key Rotation**:
1. Generate new key (Key 2)
2. Update applications to use Key 2
3. Verify applications work
4. Regenerate Key 1
5. Repeat every 90 days

**Automation**:
```bash
# Regenerate key
az cognitiveservices account keys regenerate \
  --name my-resource \
  --resource-group my-rg \
  --key-name key2
```

### 5. Use .gitignore

Ensure secrets are never committed:

```gitignore
# .gitignore
.env
appsettings.Development.json
appsettings.*.json
secrets.json
*.key
*.secret
```

### 6. Monitor Access

Enable Azure Monitor and set up alerts:

```bash
# Enable diagnostic settings
az monitor diagnostic-settings create \
  --name my-diagnostics \
  --resource /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource} \
  --logs '[{"category":"RequestResponse","enabled":true}]' \
  --workspace /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.OperationalInsights/workspaces/{workspace}
```

### 7. Use Private Endpoints

Restrict network access to Azure resources only:

```bash
# Create private endpoint
az network private-endpoint create \
  --name my-endpoint \
  --resource-group my-rg \
  --vnet-name my-vnet \
  --subnet my-subnet \
  --private-connection-resource-id /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource} \
  --group-id account \
  --connection-name my-connection
```

## Troubleshooting

### Error: "Authentication failed" (401)

**Causes**:
1. Invalid API key
2. API key not set
3. Azure Identity failed
4. Insufficient permissions

**Solutions**:

1. **Check API Key**:
   ```bash
   # Verify environment variable is set
   echo $ANTHROPIC_FOUNDRY_API_KEY

   # Check for extra spaces or line breaks
   echo "$ANTHROPIC_FOUNDRY_API_KEY" | od -c
   ```

2. **Test Azure CLI Login**:
   ```bash
   az login
   az account show
   ```

3. **Verify Managed Identity**:
   ```bash
   # Check if managed identity is enabled
   az webapp identity show --name my-app --resource-group my-rg
   ```

4. **Check RBAC Permissions**:
   ```bash
   # List role assignments
   az role assignment list \
     --assignee $principalId \
     --scope /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource}
   ```

### Error: "Failed to acquire token" (Azure Identity)

**Causes**:
1. Not logged in locally (Azure CLI)
2. Managed Identity not enabled
3. Incorrect tenant/client ID
4. Network restrictions

**Solutions**:

1. **Local Development**:
   ```bash
   az login
   az account list
   az account set --subscription "subscription-name"
   ```

2. **Check Managed Identity**:
   ```bash
   # Should return identity details
   az webapp identity show --name my-app --resource-group my-rg
   ```

3. **Enable Logging**:
   ```csharp
   var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
   {
       Diagnostics = { IsLoggingEnabled = true }
   });
   ```

4. **Try Specific Credential**:
   ```csharp
   // Instead of DefaultAzureCredential, try specific credential
   var credential = new ManagedIdentityCredential();
   ```

### Error: "Resource not found"

**Cause**: Incorrect resource name

**Solution**:
```bash
# List Anthropic Foundry resources
az cognitiveservices account list \
  --resource-group my-rg \
  --query "[?kind=='AnthropicFoundry'].name"
```

### Error: Rate limit exceeded (429)

**Solution**: Implement exponential backoff retry logic (see [API Reference](API-REFERENCE.md#issue-rate-limit-exceeded-429))

---

**Related Documentation**:
- [Getting Started Guide](GETTING-STARTED.md)
- [API Reference](API-REFERENCE.md)
- [Examples Guide](EXAMPLES-GUIDE.md)
- [Azure Managed Identity Documentation](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
