@description('Application name used for resource naming')
param appName string = 'nlwebnet'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

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

@description('Minimum number of replicas')
@minValue(0)
@maxValue(25)
param minReplicas int = 1

@description('Maximum number of replicas')
@minValue(1)
@maxValue(25)
param maxReplicas int = 3

var containerAppName = '${appName}-${environment}'
var containerAppEnvironmentName = '${appName}-env-${environment}'
var logAnalyticsWorkspaceName = '${appName}-logs-${environment}'
var appInsightsName = '${appName}-insights-${environment}'

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

// Container Apps Environment
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppEnvironmentName
  location: location
  properties: {
    daprAIInstrumentationKey: applicationInsights.properties.InstrumentationKey
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

// Container App
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
      registries: empty(containerRegistryServer) ? [] : [
        {
          server: containerRegistryServer
          username: containerRegistryUsername
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: concat(
        empty(containerRegistryPassword) ? [] : [
          {
            name: 'registry-password'
            value: containerRegistryPassword
          }
        ],
        empty(azureOpenAIApiKey) ? [] : [
          {
            name: 'azure-openai-api-key'
            value: azureOpenAIApiKey
          }
        ],
        empty(azureSearchApiKey) ? [] : [
          {
            name: 'azure-search-api-key'
            value: azureSearchApiKey
          }
        ]
      )
    }
    template: {
      containers: [
        {
          image: containerImage
          name: 'nlwebnet-demo'
          env: concat(
            [
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
                secretRef: 'azure-openai-api-key'
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
                secretRef: 'azure-search-api-key'
              }
            ]
          )
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 30
              periodSeconds: 30
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 5
              periodSeconds: 10
              timeoutSeconds: 3
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}

// Outputs
output containerAppFQDN string = containerApp.properties.configuration.ingress.fqdn
output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString