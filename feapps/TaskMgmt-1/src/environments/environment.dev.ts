export const environment = {
  production: false,
  apiBaseUrl: 'https://mdussa-task-api-cbguagdfddbmgpf3.centralindia-01.azurewebsites.net/',
  auth: {
    provider: 'azureAd' as 'local' | 'azureAd'
  },
  appInsights: {
    enabled: false,
    connectionString: 'YOUR_APP_INSIGHTS_CONNECTION_STRING',
    cloudRole: 'task-mgmt-fe-dev'
  },
  azureAd: {
    clientId: 'YOUR_AZURE_AD_APP_CLIENT_ID',
    tenantId: 'YOUR_AZURE_AD_TENANT_ID',
    redirectUri: 'http://localhost:4200/login',
    postLogoutRedirectUri: 'http://localhost:4200/login',
    loginScopes: ['openid', 'profile', 'email'],
    apiScopes: ['api://YOUR_API_APP_ID_URI/access_as_user']
  }
};