export const environment = {
  production: false,
  apiBaseUrl: 'https://your-qa-api-url/',
  auth: {
    provider: 'azureAd' as 'local' | 'azureAd'
  },
  appInsights: {
    enabled: false,
    connectionString: 'YOUR_QA_APP_INSIGHTS_CONNECTION_STRING',
    cloudRole: 'task-mgmt-fe-qa'
  },
  azureAd: {
    clientId: 'YOUR_QA_AZURE_AD_APP_CLIENT_ID',
    tenantId: 'YOUR_QA_AZURE_AD_TENANT_ID',
    redirectUri: 'https://your-qa-app-url/login',
    postLogoutRedirectUri: 'https://your-qa-app-url/login',
    loginScopes: ['openid', 'profile', 'email'],
    apiScopes: ['api://YOUR_QA_API_APP_ID_URI/access_as_user']
  }
};