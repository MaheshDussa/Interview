# TaskMgmt

See [AZURE-RESOURCES-SETUP.md](AZURE-RESOURCES-SETUP.md) for the Azure resources required to run the application across environments and how each service is used.

This Angular 18 app supports two authentication modes:

- `local`: the original email-based login against `/api/Auth/login`
- `azureAd`: Microsoft Entra ID sign-in through `@azure/msal-browser`

The active mode is selected in `src/environments/environment.ts`.

## Authentication mode selection

Set the auth mode in `src/environments/environment.ts`:

```ts
auth: {
	provider: 'local' // or 'azureAd'
}
```

Mode behavior:

- `local`: keeps the existing login screen and stores the API token returned by `/api/Auth/login`.
- `azureAd`: shows the Microsoft sign-in button and acquires an access token for the configured API scope.

## Environment files

The app now has separate environment files for each target:

- `src/environments/environment.ts`: local fallback/default
- `src/environments/environment.dev.ts`: development
- `src/environments/environment.qa.ts`: QA
- `src/environments/environment.stage.ts`: stage
- `src/environments/environment.prod.ts`: production

Angular configurations now map to those files:

- `ng serve --configuration development`
- `ng serve --configuration qa`
- `ng serve --configuration stage`
- `ng build --configuration production`

Update the API URL, auth mode, Azure AD values, and App Insights connection string in each file for the corresponding environment.

## Application Insights logging

The app now includes a filtered Application Insights integration intended to log your app's code paths without enabling the SDK's broad automatic browser error collection.

Enable it in `src/environments/environment.ts`:

```ts
appInsights: {
	enabled: true,
	connectionString: 'InstrumentationKey=...;IngestionEndpoint=...;',
	cloudRole: 'task-mgmt-fe'
}
```

What gets logged:

- Angular startup failures from `main.ts`
- Unhandled Angular errors through a custom global `ErrorHandler`
- API request failures from the app's HTTP interceptor
- Login, logout, and task operation failures from the app's own components
- Angular route changes as page views

What is intentionally not enabled:

- automatic AJAX/fetch collection
- automatic exception collection for every browser/library error
- broad framework noise from third-party dependencies

Filtering rule:

- global exception logging is filtered so only errors that look like app-owned code paths are sent
- explicit error tracking from your own components and services is still sent

This keeps telemetry focused on your code issues instead of generic library/runtime noise.

## Azure AD setup

- The old email-only login flow was replaced with Azure AD popup sign-in.
- Authentication state is restored from the Microsoft account cache on refresh.
- API calls to `environment.apiBaseUrl` automatically receive an Azure AD access token.
- Sign-out clears the Microsoft session from the SPA.

## 1. Create the Azure app registrations

You need two Azure app registrations if your task API is also protected by Azure AD:

1. Create a **SPA app registration** for this Angular frontend.
2. Create or reuse an **API app registration** for the backend.

For the SPA app registration:

1. Go to Azure Portal -> Microsoft Entra ID -> App registrations -> New registration.
2. Set a name such as `TaskMgmt SPA`.
3. Choose the tenant type your users will sign in from.
4. Add a redirect URI of type `Single-page application`.
5. Use `http://localhost:4200/login` for local development.
6. Add your deployed login URL too, for example `https://your-app-domain/login`.

For the API app registration:

1. Open the API app registration.
2. Go to `Expose an API`.
3. Set the Application ID URI if it is not already defined.
4. Add a scope such as `access_as_user`.
5. Copy the scope value, for example `api://<api-client-id>/access_as_user`.

To allow the SPA to call the API:

1. Open the SPA app registration.
2. Go to `API permissions`.
3. Add the delegated permission for the API scope you created.
4. Grant admin consent if your tenant requires it.

## 2. Update the frontend configuration

Edit `src/environments/environment.ts` and replace the placeholder values in `azureAd`:

```ts
azureAd: {
	clientId: 'YOUR_AZURE_AD_APP_CLIENT_ID',
	tenantId: 'YOUR_AZURE_AD_TENANT_ID',
	redirectUri: 'http://localhost:4200/login',
	postLogoutRedirectUri: 'http://localhost:4200/login',
	loginScopes: ['openid', 'profile', 'email'],
	apiScopes: ['api://YOUR_API_APP_ID_URI/access_as_user']
}
```

Field meanings:

- `clientId`: the Application (client) ID of the SPA registration.
- `tenantId`: the Directory (tenant) ID.
- `redirectUri`: the route Azure returns to after sign-in.
- `postLogoutRedirectUri`: where the user lands after sign-out.
- `loginScopes`: basic Microsoft identity scopes.
- `apiScopes`: delegated scopes for the task API.

## 3. Backend requirement

This frontend now sends Azure AD bearer tokens to the task API. The backend must validate those tokens with the same tenant and audience values that match your API app registration.

Typical backend requirements:

1. Configure JWT bearer authentication against Microsoft Entra ID.
2. Set the valid audience to the API application's client ID or Application ID URI.
3. Require the delegated scope exposed by the API, such as `access_as_user`.

If the backend still expects the old custom token from `/api/Auth/login`, task requests will fail until the backend is updated to trust Azure AD tokens.

If you keep `auth.provider: 'local'`, the backend can continue using the existing `/api/Auth/login` flow unchanged.

## 4. Install and run

```bash
npm install
npm start
```

Open `http://localhost:4200`, select `Sign in with Microsoft`, complete the Azure sign-in flow, and the app will navigate to the protected tasks page.

## Build

```bash
npm run build
```

Current status: the project builds successfully after the Azure AD integration. The existing Angular size warnings still remain in the build output.
