@description('Application name used for resource naming')
param appName string = 'nlwebnet'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('App Service Plan SKU')
@allowed(['F1', 'D1', 'B1', 'B2', 'B3', 'S1', 'S2', 'S3', 'P1V2', 'P2V2', 'P3V2', 'P1V3', 'P2V3', 'P3V3'])
param appServicePlanSku string = 'B1'

@description('Container image name and tag')
param containerImage string = 'nlwebnet-demo:latest'

@description('Container registry server')
param containerRegistryServer string = ''

@description('Container registry username')
@secure()
param containerRegistryUsername string = ''

@description('Container registry password')
@secure()
param containerRegistryPassword string = ''

@description('Azure OpenAI API Key')
@secure()
param azureOpenAIApiKey string = ''

@description('Azure OpenAI Endpoint')
param azureOpenAIEndpoint string = ''

@description('Azure OpenAI Deployment Name')
param azureOpenAIDeploymentName string = 'gpt-4'

@description('Azure Search API Key')
@secure()
param azureSearchApiKey string = ''

@description('Azure Search Service Name')
param azureSearchServiceName string = ''

@description('Azure Search Index Name')
param azureSearchIndexName string = 'nlweb-index'

var appServicePlanName = '${appName}-plan-${environment}'
var appServiceName = '${appName}-app-${environment}'
var appInsightsName = '${appName}-insights-${environment}'
var logAnalyticsWorkspaceName = '${appName}-logs-${environment}'

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
    tier: appServicePlanSku == 'F1' ? 'Free' : appServicePlanSku == 'D1' ? 'Shared' : contains(appServicePlanSku, 'B') ? 'Basic' : contains(appServicePlanSku, 'S') ? 'Standard' : 'PremiumV2'
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux plans
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerImage}'
      appCommandLine: ''
      alwaysOn: appServicePlanSku != 'F1' && appServicePlanSku != 'D1' // Not supported on Free/Shared tiers
      httpLoggingEnabled: true
      logsDirectorySizeLimit: 35
      detailedErrorLoggingEnabled: true
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: concat(
        [
          {
            name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
            value: 'false'
          }
          {
            name: 'ASPNETCORE_ENVIRONMENT'
            value: environment == 'prod' ? 'Production' : 'Development'
          }
          {
            name: 'ASPNETCORE_URLS'
            value: 'http://+:8080'
          }
          {
            name: 'NLWebNet__DefaultMode'
            value: 'List'
          }
          {
            name: 'NLWebNet__EnableStreaming'
            value: 'true'
          }
          {
            name: 'NLWebNet__DefaultTimeoutSeconds'
            value: '30'
          }
          {
            name: 'NLWebNet__MaxResultsPerQuery'
            value: '50'
          }
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: applicationInsights.properties.ConnectionString
          }
          {
            name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
            value: '~3'
          }
          {
            name: 'APPINSIGHTS_PROFILERFEATURE_VERSION'
            value: '1.0.0'
          }
          {
            name: 'APPINSIGHTS_SNAPSHOTFEATURE_VERSION'
            value: '1.0.0'
          }
        ],
        empty(containerRegistryServer) ? [] : [
          {
            name: 'DOCKER_REGISTRY_SERVER_URL'
            value: 'https://${containerRegistryServer}'
          }
          {
            name: 'DOCKER_REGISTRY_SERVER_USERNAME'
            value: containerRegistryUsername
          }
          {
            name: 'DOCKER_REGISTRY_SERVER_PASSWORD'
            value: containerRegistryPassword
          }
        ],
        empty(azureOpenAIEndpoint) ? [] : [
          {
            name: 'AzureOpenAI__Endpoint'
            value: azureOpenAIEndpoint
          }
          {
            name: 'AzureOpenAI__DeploymentName'
            value: azureOpenAIDeploymentName
          }
        ],
        empty(azureOpenAIApiKey) ? [] : [
          {
            name: 'AzureOpenAI__ApiKey'
            value: azureOpenAIApiKey
          }
        ],
        empty(azureSearchServiceName) ? [] : [
          {
            name: 'AzureSearch__ServiceName'
            value: azureSearchServiceName
          }
          {
            name: 'AzureSearch__IndexName'
            value: azureSearchIndexName
          }
        ],
        empty(azureSearchApiKey) ? [] : [
          {
            name: 'AzureSearch__ApiKey'
            value: azureSearchApiKey
          }
        ]
      )
    }
    httpsOnly: true
  }
}

// Configure health check endpoint
resource healthCheckConfig 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: appService
  name: 'web'
  properties: {
    healthCheckPath: '/health'
  }
}

// Outputs
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServiceName string = appService.name
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString