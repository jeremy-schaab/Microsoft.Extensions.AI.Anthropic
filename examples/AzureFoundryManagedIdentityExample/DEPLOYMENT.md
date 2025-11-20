# Deployment Guide - Azure Foundry Managed Identity Example

Comprehensive deployment guide for production Azure environments.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Azure App Service](#azure-app-service)
3. [Azure Functions](#azure-functions)
4. [Azure Kubernetes Service](#azure-kubernetes-service)
5. [Azure Container Apps](#azure-container-apps)
6. [CI/CD Pipeline](#cicd-pipeline)
7. [Infrastructure as Code](#infrastructure-as-code)

## Quick Start

### Prerequisites Checklist

- [ ] Azure subscription with Owner or Contributor role
- [ ] Azure Anthropic Foundry resource deployed
- [ ] .NET 9.0 SDK installed
- [ ] Azure CLI installed and logged in (`az login`)
- [ ] Git repository for CI/CD

### 5-Minute Deployment (App Service)

```bash
# Set variables
RESOURCE_GROUP="rg-anthropic-prod"
APP_NAME="app-anthropic-$(openssl rand -hex 4)"
LOCATION="eastus"
FOUNDRY_RESOURCE_NAME="your-foundry-resource"
FOUNDRY_RG="rg-foundry"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service
az webapp up \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --runtime "DOTNET:9.0" \
  --sku B1 \
  --location $LOCATION

# Enable managed identity
az webapp identity assign --name $APP_NAME --resource-group $RESOURCE_GROUP

# Get principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Get Foundry resource ID
FOUNDRY_ID=$(az resource show \
  --name $FOUNDRY_RESOURCE_NAME \
  --resource-type "Microsoft.MachineLearningServices/foundries" \
  --resource-group $FOUNDRY_RG \
  --query id -o tsv)

# Assign RBAC role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_ID

# Configure app settings
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    AnthropicFoundry__ResourceName=$FOUNDRY_RESOURCE_NAME \
    AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022"

echo "Deployment complete: https://$APP_NAME.azurewebsites.net"
```

## Azure App Service

### Production Deployment with User-Assigned Identity

#### Step 1: Create Infrastructure

```bash
# Variables
RESOURCE_GROUP="rg-anthropic-prod"
LOCATION="eastus"
APP_SERVICE_PLAN="plan-anthropic-prod"
APP_NAME="app-anthropic-prod"
IDENTITY_NAME="id-anthropic-prod"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION \
  --tags environment=production application=anthropic

# Create user-assigned managed identity
az identity create \
  --name $IDENTITY_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Get identity details
IDENTITY_ID=$(az identity show \
  --name $IDENTITY_NAME \
  --resource-group $RESOURCE_GROUP \
  --query id -o tsv)

PRINCIPAL_ID=$(az identity show \
  --name $IDENTITY_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

CLIENT_ID=$(az identity show \
  --name $IDENTITY_NAME \
  --resource-group $RESOURCE_GROUP \
  --query clientId -o tsv)

# Create App Service Plan (Production tier)
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku P1V3 \
  --is-linux \
  --number-of-workers 2

# Create App Service
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNET:9.0" \
  --assign-identity $IDENTITY_ID
```

#### Step 2: Configure RBAC

```bash
# Get Foundry resource ID
FOUNDRY_RESOURCE_NAME="your-foundry-resource"
FOUNDRY_RG="rg-foundry-prod"

FOUNDRY_ID=$(az resource show \
  --name $FOUNDRY_RESOURCE_NAME \
  --resource-type "Microsoft.MachineLearningServices/foundries" \
  --resource-group $FOUNDRY_RG \
  --query id -o tsv)

# Assign role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_ID \
  --description "Anthropic App Service access to Foundry"
```

#### Step 3: Configure Application

```bash
# Application settings
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    AnthropicFoundry__ResourceName=$FOUNDRY_RESOURCE_NAME \
    AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022" \
    AnthropicFoundry__ManagedIdentityClientId=$CLIENT_ID

# Enable Application Insights
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    APPLICATIONINSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show \
      --app "ai-anthropic-prod" \
      --resource-group $RESOURCE_GROUP \
      --query connectionString -o tsv)

# Configure health check
az webapp config set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --health-check-path "/health"

# Enable auto-scaling
az monitor autoscale create \
  --name "autoscale-anthropic" \
  --resource-group $RESOURCE_GROUP \
  --resource $APP_SERVICE_PLAN \
  --resource-type "Microsoft.Web/serverfarms" \
  --min-count 2 \
  --max-count 10 \
  --count 2

# Add CPU scaling rule
az monitor autoscale rule create \
  --autoscale-name "autoscale-anthropic" \
  --resource-group $RESOURCE_GROUP \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 2
```

#### Step 4: Deploy Application

```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy
az webapp deployment source config-zip \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src deploy.zip

# Verify deployment
az webapp browse --name $APP_NAME --resource-group $RESOURCE_GROUP
```

#### Step 5: Configure Networking (Optional)

```bash
# Enable private endpoint
VNET_NAME="vnet-anthropic-prod"
SUBNET_NAME="subnet-app-service"

az network vnet create \
  --name $VNET_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --address-prefix 10.0.0.0/16

az network vnet subnet create \
  --name $SUBNET_NAME \
  --resource-group $RESOURCE_GROUP \
  --vnet-name $VNET_NAME \
  --address-prefix 10.0.1.0/24

# Enable VNet integration
az webapp vnet-integration add \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --vnet $VNET_NAME \
  --subnet $SUBNET_NAME
```

## Azure Functions

### Serverless Deployment

#### Step 1: Create Function App

```bash
# Variables
FUNC_APP_NAME="func-anthropic-prod"
STORAGE_ACCOUNT="stanthropic$(openssl rand -hex 4)"

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false

# Create Function App with managed identity
az functionapp create \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --storage-account $STORAGE_ACCOUNT \
  --runtime dotnet-isolated \
  --runtime-version 9.0 \
  --functions-version 4 \
  --os-type Linux \
  --consumption-plan-location $LOCATION \
  --assign-identity [system]
```

#### Step 2: Configure Managed Identity

```bash
# Get principal ID
FUNC_PRINCIPAL_ID=$(az functionapp identity show \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Assign RBAC role
az role assignment create \
  --assignee $FUNC_PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_ID
```

#### Step 3: Configure Application Settings

```bash
az functionapp config appsettings set \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    AnthropicFoundry__ResourceName=$FOUNDRY_RESOURCE_NAME \
    AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022" \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
```

#### Step 4: Deploy Function

```bash
# Using Azure Functions Core Tools
func azure functionapp publish $FUNC_APP_NAME

# Or using zip deployment
func azure functionapp publish $FUNC_APP_NAME --build remote
```

## Azure Kubernetes Service

### Container Orchestration Deployment

#### Step 1: Create AKS Cluster

```bash
# Variables
AKS_NAME="aks-anthropic-prod"
NODE_COUNT=3
VM_SIZE="Standard_D4s_v3"

# Create AKS with Workload Identity
az aks create \
  --name $AKS_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --node-count $NODE_COUNT \
  --node-vm-size $VM_SIZE \
  --enable-managed-identity \
  --enable-oidc-issuer \
  --enable-workload-identity \
  --enable-addons monitoring \
  --network-plugin azure \
  --network-policy azure \
  --generate-ssh-keys

# Get credentials
az aks get-credentials \
  --name $AKS_NAME \
  --resource-group $RESOURCE_GROUP \
  --overwrite-existing
```

#### Step 2: Create Azure Container Registry

```bash
# Create ACR
ACR_NAME="acranthropic$(openssl rand -hex 4)"

az acr create \
  --name $ACR_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku Premium \
  --location $LOCATION

# Attach ACR to AKS
az aks update \
  --name $AKS_NAME \
  --resource-group $RESOURCE_GROUP \
  --attach-acr $ACR_NAME
```

#### Step 3: Build and Push Container Image

```bash
# Build image
az acr build \
  --registry $ACR_NAME \
  --image anthropic-app:latest \
  --file Dockerfile \
  .

# Or using Docker
docker build -t $ACR_NAME.azurecr.io/anthropic-app:latest .
az acr login --name $ACR_NAME
docker push $ACR_NAME.azurecr.io/anthropic-app:latest
```

#### Step 4: Create Workload Identity

```bash
# Create user-assigned identity
az identity create \
  --name "id-anthropic-workload" \
  --resource-group $RESOURCE_GROUP

WORKLOAD_IDENTITY_CLIENT_ID=$(az identity show \
  --name "id-anthropic-workload" \
  --resource-group $RESOURCE_GROUP \
  --query clientId -o tsv)

WORKLOAD_PRINCIPAL_ID=$(az identity show \
  --name "id-anthropic-workload" \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Assign RBAC role
az role assignment create \
  --assignee $WORKLOAD_PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_ID

# Get AKS OIDC issuer
AKS_OIDC_ISSUER=$(az aks show \
  --name $AKS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query oidcIssuerProfile.issuerUrl -o tsv)

# Create federated credential
az identity federated-credential create \
  --name "fc-anthropic-workload" \
  --identity-name "id-anthropic-workload" \
  --resource-group $RESOURCE_GROUP \
  --issuer $AKS_OIDC_ISSUER \
  --subject "system:serviceaccount:anthropic:anthropic-sa" \
  --audience api://AzureADTokenExchange
```

#### Step 5: Deploy to Kubernetes

Create namespace:
```bash
kubectl create namespace anthropic
```

Create `manifests/serviceaccount.yaml`:
```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: anthropic-sa
  namespace: anthropic
  annotations:
    azure.workload.identity/client-id: "${WORKLOAD_IDENTITY_CLIENT_ID}"
```

Create `manifests/configmap.yaml`:
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: anthropic-config
  namespace: anthropic
data:
  AnthropicFoundry__ResourceName: "${FOUNDRY_RESOURCE_NAME}"
  AnthropicFoundry__ModelId: "claude-3-5-sonnet-20241022"
```

Create `manifests/deployment.yaml`:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: anthropic-app
  namespace: anthropic
spec:
  replicas: 3
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
        image: ${ACR_NAME}.azurecr.io/anthropic-app:latest
        envFrom:
        - configMapRef:
            name: anthropic-config
        ports:
        - containerPort: 8080
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: anthropic-service
  namespace: anthropic
spec:
  type: LoadBalancer
  selector:
    app: anthropic-app
  ports:
  - port: 80
    targetPort: 8080
```

Deploy:
```bash
# Replace variables in manifests
export WORKLOAD_IDENTITY_CLIENT_ID ACR_NAME FOUNDRY_RESOURCE_NAME
envsubst < manifests/serviceaccount.yaml | kubectl apply -f -
envsubst < manifests/configmap.yaml | kubectl apply -f -
envsubst < manifests/deployment.yaml | kubectl apply -f -

# Verify deployment
kubectl get pods -n anthropic
kubectl get svc -n anthropic
```

## Azure Container Apps

### Modern Serverless Containers

#### Step 1: Create Environment

```bash
# Variables
CONTAINERAPPS_ENV="env-anthropic-prod"
CONTAINERAPPS_APP="app-anthropic-prod"
LOG_ANALYTICS_WORKSPACE="log-anthropic-prod"

# Create Log Analytics workspace
az monitor log-analytics workspace create \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LOG_ANALYTICS_WORKSPACE \
  --location $LOCATION

# Get workspace details
LOG_ANALYTICS_WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LOG_ANALYTICS_WORKSPACE \
  --query customerId -o tsv)

LOG_ANALYTICS_WORKSPACE_KEY=$(az monitor log-analytics workspace get-shared-keys \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LOG_ANALYTICS_WORKSPACE \
  --query primarySharedKey -o tsv)

# Create Container Apps environment
az containerapp env create \
  --name $CONTAINERAPPS_ENV \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --logs-workspace-id $LOG_ANALYTICS_WORKSPACE_ID \
  --logs-workspace-key $LOG_ANALYTICS_WORKSPACE_KEY
```

#### Step 2: Create Managed Identity

```bash
az identity create \
  --name "id-anthropic-containerapp" \
  --resource-group $RESOURCE_GROUP

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

# Assign RBAC role
az role assignment create \
  --assignee $CONTAINERAPP_PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope $FOUNDRY_ID
```

#### Step 3: Deploy Container App

```bash
az containerapp create \
  --name $CONTAINERAPPS_APP \
  --resource-group $RESOURCE_GROUP \
  --environment $CONTAINERAPPS_ENV \
  --image $ACR_NAME.azurecr.io/anthropic-app:latest \
  --target-port 8080 \
  --ingress external \
  --user-assigned $CONTAINERAPP_IDENTITY_ID \
  --registry-server $ACR_NAME.azurecr.io \
  --registry-identity $CONTAINERAPP_IDENTITY_ID \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    AnthropicFoundry__ResourceName=$FOUNDRY_RESOURCE_NAME \
    AnthropicFoundry__ModelId="claude-3-5-sonnet-20241022" \
    AnthropicFoundry__ManagedIdentityClientId=$CONTAINERAPP_CLIENT_ID \
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 1 \
  --max-replicas 10 \
  --scale-rule-name http-scaling \
  --scale-rule-type http \
  --scale-rule-http-concurrency 100
```

## CI/CD Pipeline

### Azure DevOps Pipeline

Create `azure-pipelines.yml`:

```yaml
trigger:
  branches:
    include:
    - main
  paths:
    include:
    - examples/AzureFoundryManagedIdentityExample/**

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  projectPath: 'examples/AzureFoundryManagedIdentityExample'

stages:
- stage: Build
  jobs:
  - job: BuildAndTest
    steps:
    - task: UseDotNet@2
      inputs:
        version: '9.0.x'

    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
        projects: '$(projectPath)/**/*.csproj'

    - task: DotNetCoreCLI@2
      displayName: 'Build project'
      inputs:
        command: 'build'
        projects: '$(projectPath)/**/*.csproj'
        arguments: '--configuration $(buildConfiguration)'

    - task: DotNetCoreCLI@2
      displayName: 'Publish project'
      inputs:
        command: 'publish'
        publishWebProjects: false
        projects: '$(projectPath)/**/*.csproj'
        arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)'

    - task: PublishBuildArtifacts@1
      inputs:
        pathToPublish: '$(Build.ArtifactStagingDirectory)'
        artifactName: 'drop'

- stage: DeployDev
  dependsOn: Build
  condition: succeeded()
  jobs:
  - deployment: DeployToAppService
    environment: 'dev'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: '<service-connection>'
              appName: 'app-anthropic-dev'
              package: '$(Pipeline.Workspace)/drop/**/*.zip'

- stage: DeployProd
  dependsOn: DeployDev
  condition: succeeded()
  jobs:
  - deployment: DeployToProduction
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: '<service-connection>'
              appName: 'app-anthropic-prod'
              package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

### GitHub Actions Workflow

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
    paths:
      - 'examples/AzureFoundryManagedIdentityExample/**'
  workflow_dispatch:

env:
  DOTNET_VERSION: '9.0.x'

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore examples/AzureFoundryManagedIdentityExample

    - name: Build
      run: dotnet build examples/AzureFoundryManagedIdentityExample --configuration Release --no-restore

    - name: Publish
      run: dotnet publish examples/AzureFoundryManagedIdentityExample --configuration Release --output ./publish

    - name: Upload artifact
      uses: actions/upload-artifact@v4
      with:
        name: app
        path: ./publish

  deploy-dev:
    needs: build
    runs-on: ubuntu-latest
    environment: development

    steps:
    - name: Download artifact
      uses: actions/download-artifact@v4
      with:
        name: app

    - name: Login to Azure
      uses: azure/login@v2
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Deploy to App Service
      uses: azure/webapps-deploy@v3
      with:
        app-name: 'app-anthropic-dev'
        package: .

  deploy-prod:
    needs: deploy-dev
    runs-on: ubuntu-latest
    environment: production

    steps:
    - name: Download artifact
      uses: actions/download-artifact@v4
      with:
        name: app

    - name: Login to Azure
      uses: azure/login@v2
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Deploy to App Service
      uses: azure/webapps-deploy@v3
      with:
        app-name: 'app-anthropic-prod'
        package: .
```

## Infrastructure as Code

### Bicep Template

Create `infrastructure/main.bicep`:

```bicep
@description('Environment name')
param environmentName string = 'prod'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Anthropic Foundry resource name')
param foundryResourceName string

@description('Foundry resource group')
param foundryResourceGroup string

var appServicePlanName = 'plan-anthropic-${environmentName}'
var appServiceName = 'app-anthropic-${environmentName}'
var identityName = 'id-anthropic-${environmentName}'

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'P1v3'
    tier: 'PremiumV3'
    capacity: 2
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET|9.0'
      appSettings: [
        {
          name: 'AnthropicFoundry__ResourceName'
          value: foundryResourceName
        }
        {
          name: 'AnthropicFoundry__ModelId'
          value: 'claude-3-5-sonnet-20241022'
        }
        {
          name: 'AnthropicFoundry__ManagedIdentityClientId'
          value: identity.properties.clientId
        }
      ]
    }
  }
}

// Assign RBAC role to Foundry resource
resource foundryResource 'Microsoft.MachineLearningServices/foundries@2023-10-01' existing = {
  name: foundryResourceName
  scope: resourceGroup(foundryResourceGroup)
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryResource.id, identity.id, 'CognitiveServicesUser')
  scope: foundryResource
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908') // Cognitive Services User
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output identityClientId string = identity.properties.clientId
```

Deploy:
```bash
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infrastructure/main.bicep \
  --parameters \
    environmentName=prod \
    foundryResourceName=$FOUNDRY_RESOURCE_NAME \
    foundryResourceGroup=$FOUNDRY_RG
```

## Monitoring and Diagnostics

### Enable Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app "ai-anthropic-prod" \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web

# Get connection string
AI_CONNECTION_STRING=$(az monitor app-insights component show \
  --app "ai-anthropic-prod" \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv)

# Add to App Service
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    APPLICATIONINSIGHTS_CONNECTION_STRING=$AI_CONNECTION_STRING
```

### Configure Diagnostic Logs

```bash
# Create storage account for logs
az storage account create \
  --name "stlogs$(openssl rand -hex 4)" \
  --resource-group $RESOURCE_GROUP \
  --sku Standard_LRS

# Enable diagnostic settings
az monitor diagnostic-settings create \
  --name "diag-anthropic" \
  --resource $(az webapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query id -o tsv) \
  --logs '[{"category": "AppServiceHTTPLogs", "enabled": true}, {"category": "AppServiceConsoleLogs", "enabled": true}]' \
  --metrics '[{"category": "AllMetrics", "enabled": true}]'
```

## Backup and Disaster Recovery

### App Service Backup

```bash
# Create storage account for backups
az storage account create \
  --name "stbackup$(openssl rand -hex 4)" \
  --resource-group $RESOURCE_GROUP \
  --sku Standard_GRS

# Configure backup
az webapp config backup create \
  --resource-group $RESOURCE_GROUP \
  --webapp-name $APP_NAME \
  --container-url "<sas-url>" \
  --backup-name "anthropic-backup" \
  --retain-one true \
  --frequency 1d
```

## Production Checklist

- [ ] Managed identity enabled and configured
- [ ] RBAC roles assigned with least privilege
- [ ] Application Insights configured
- [ ] Diagnostic logging enabled
- [ ] Health checks implemented
- [ ] Auto-scaling configured
- [ ] Backup strategy in place
- [ ] Network security (VNet, private endpoints)
- [ ] CI/CD pipeline tested
- [ ] Monitoring and alerts configured
- [ ] Documentation updated
- [ ] Disaster recovery plan documented
- [ ] Security scan completed
- [ ] Performance testing completed

## Support

For deployment issues:
- Check Azure Portal diagnostics
- Review Application Insights logs
- Enable verbose logging in appsettings.json
- Verify RBAC role assignments
- Check managed identity configuration
