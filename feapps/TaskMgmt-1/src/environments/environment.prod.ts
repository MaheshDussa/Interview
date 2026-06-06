export const environment = {
  production: true,
  apiBaseUrl: 'https://your-prod-api-url/',
  auth: {
    provider: 'azureAd' as 'local' | 'azureAd'
  },
  appInsights: {
    enabled: false,
    connectionString: 'YOUR_PROD_APP_INSIGHTS_CONNECTION_STRING',
    cloudRole: 'task-mgmt-fe-prod'
  },
  azureAd: {
    clientId: 'YOUR_PROD_AZURE_AD_APP_CLIENT_ID',
    tenantId: 'YOUR_PROD_AZURE_AD_TENANT_ID',
    redirectUri: 'https://your-prod-app-url/login',
    postLogoutRedirectUri: 'https://your-prod-app-url/login',
    loginScopes: ['openid', 'profile', 'email'],
    apiScopes: ['api://YOUR_PROD_API_APP_ID_URI/access_as_user']
  }
};