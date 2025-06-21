// Deploy to Azure Container Apps
targetScope = 'resourceGroup'

@description('Name of the Container App')
param containerAppName string = 'nlwebnet'

@description('Name of the Container App Environment')
param environmentName string = 'nlwebnet-env'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Container image to deploy')
param containerImage string = 'nlwebnet:latest'

@description('Azure OpenAI API Key')
@secure()
param azureOpenAIApiKey string

@description('Azure OpenAI Endpoint')
param azureOpenAIEndpoint string

@description('Azure Search API Key')
@secure()
param azureSearchApiKey string

@description('Azure Search Service Name')
param azureSearchServiceName string

@description('Tags to apply to resources')
param tags object = {
  environment: 'production'
  application: 'nlwebnet'
  deployedBy: 'bicep'
}

var logAnalyticsWorkspaceName = '${containerAppName}-logs'

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Container App Environment
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
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
resource containerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: containerAppName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      secrets: [
        {
          name: 'azure-openai-api-key'
          value: azureOpenAIApiKey
        }
        {
          name: 'azure-search-api-key'
          value: azureSearchApiKey
        }
      ]
    }
    template: {
      revisionSuffix: 'v1'
      containers: [
        {
          name: 'nlwebnet'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
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
              name: 'NLWebNet__RateLimiting__RequestsPerWindow'
              value: '1000'
            }
            {
              name: 'NLWebNet__RateLimiting__WindowSizeInMinutes'
              value: '1'
            }
            {
              name: 'AzureOpenAI__ApiKey'
              secretRef: 'azure-openai-api-key'
            }
            {
              name: 'AzureOpenAI__Endpoint'
              value: azureOpenAIEndpoint
            }
            {
              name: 'AzureOpenAI__DeploymentName'
              value: 'gpt-4'
            }
            {
              name: 'AzureSearch__ApiKey'
              secretRef: 'azure-search-api-key'
            }
            {
              name: 'AzureSearch__ServiceName'
              value: azureSearchServiceName
            }
            {
              name: 'AzureSearch__IndexName'
              value: 'nlweb-index'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 30
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 5
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
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
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id