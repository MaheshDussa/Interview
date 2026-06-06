export const environment = {
  production: false,
  apiBaseUrl: 'https://your-stage-api-url/',
  auth: {
    provider: 'azureAd' as 'local' | 'azureAd'
  },
  appInsights: {
    enabled: false,
    connectionString: 'YOUR_STAGE_APP_INSIGHTS_CONNECTION_STRING',
    cloudRole: 'task-mgmt-fe-stage'
  },
  azureAd: {
    clientId: 'YOUR_STAGE_AZURE_AD_APP_CLIENT_ID',
    tenantId: 'YOUR_STAGE_AZURE_AD_TENANT_ID',
    redirectUri: 'https://your-stage-app-url/login',
    postLogoutRedirectUri: 'https://your-stage-app-url/login',
    loginScopes: ['openid', 'profile', 'email'],
    apiScopes: ['api://YOUR_STAGE_API_APP_ID_URI/access_as_user']
  }
};