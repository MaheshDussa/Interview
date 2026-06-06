import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import {
  AccountInfo,
  AuthenticationResult,
  InteractionRequiredAuthError,
  PublicClientApplication
} from '@azure/msal-browser';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginRequest, LoginResponse } from './models';

const TOKEN_KEY = 'tm_token';
const USER_KEY = 'tm_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly http = inject(HttpClient);
  private readonly browser = isPlatformBrowser(this.platformId);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/+$/, '');
  private readonly authProvider = environment.auth.provider;
  private readonly azureAd = environment.azureAd;
  private msal: PublicClientApplication | null = null;
  private initialization: Promise<void> | null = null;
  private readonly account = signal<AccountInfo | null>(null);
  private readonly localUser = signal<string | null>(this.getStoredUser());
  private readonly localAuthenticated = signal<boolean>(this.hasLocalToken());

  readonly isAuthenticated = computed(() => this.usesAzureAd() ? !!this.account() : this.localAuthenticated());
  readonly userDisplayName = computed(() => {
    if (!this.usesAzureAd()) {
      return this.localUser();
    }

    const currentAccount = this.account();
    return currentAccount?.name || currentAccount?.username || null;
  });

  async initialize(): Promise<void> {
    if (!this.browser || !this.usesAzureAd() || !this.isConfigured()) {
      return;
    }

    if (!this.initialization) {
      this.initialization = (async () => {
        const msal = this.getMsal();
        await msal.initialize();
        await msal.handleRedirectPromise();
        this.syncAccountState();
      })();
    }

    await this.initialization;
  }

  loginWithEmail(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiBaseUrl}/api/Auth/login`, payload).pipe(
      tap(res => {
        const token = res?.token ?? res?.accessToken ?? res?.jwt;
        if (token) {
          this.setLocalSession(token, payload.email);
        }
      })
    );
  }

  async loginWithAzureAd(): Promise<void> {
    this.assertConfigured();
    await this.initialize();

    const result = await this.getMsal().loginPopup({
      scopes: this.getLoginScopes(),
      prompt: 'select_account'
    });

    this.setActiveAccount(result);
  }

  async logout(): Promise<void> {
    if (!this.usesAzureAd()) {
      this.clearLocalSession();
      return;
    }

    if (!this.browser || !this.isConfigured()) {
      this.account.set(null);
      return;
    }

    await this.initialize();
    const currentAccount = this.account();

    await this.getMsal().logoutPopup({
      account: currentAccount ?? undefined,
      postLogoutRedirectUri: this.resolvePostLogoutRedirectUri(),
      mainWindowRedirectUri: this.resolvePostLogoutRedirectUri()
    });

    this.account.set(null);
  }

  async acquireAccessToken(): Promise<string | null> {
    if (!this.usesAzureAd()) {
      return this.getLocalToken();
    }

    if (!this.browser || !this.isConfigured()) {
      return null;
    }

    await this.initialize();
    const activeAccount = this.account() ?? this.getMsal().getActiveAccount() ?? this.getMsal().getAllAccounts()[0] ?? null;
    if (!activeAccount) {
      return null;
    }

    try {
      const result = await this.getMsal().acquireTokenSilent({
        account: activeAccount,
        scopes: this.getApiScopes()
      });
      this.setActiveAccount(result);
      return result.accessToken;
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        const result = await this.getMsal().acquireTokenPopup({
          account: activeAccount,
          scopes: this.getApiScopes()
        });
        this.setActiveAccount(result);
        return result.accessToken;
      }

      throw error;
    }
  }

  isConfigured(): boolean {
    if (!this.usesAzureAd()) {
      return true;
    }

    return this.isValueConfigured(this.azureAd.clientId)
      && this.isValueConfigured(this.azureAd.tenantId)
      && this.getApiScopes().length > 0;
  }

  usesAzureAd(): boolean {
    return this.authProvider === 'azureAd';
  }

  usesLocalAuth(): boolean {
    return this.authProvider === 'local';
  }

  shouldAttachToken(url: string): boolean {
    return url.startsWith(this.apiBaseUrl);
  }

  private getMsal(): PublicClientApplication {
    if (!this.msal) {
      this.msal = new PublicClientApplication({
        auth: {
          clientId: this.azureAd.clientId,
          authority: `https://login.microsoftonline.com/${this.azureAd.tenantId}`,
          redirectUri: this.resolveRedirectUri(),
          postLogoutRedirectUri: this.resolvePostLogoutRedirectUri()
        },
        cache: {
          cacheLocation: 'localStorage'
        }
      });
    }

    return this.msal;
  }

  private syncAccountState(): void {
    const msal = this.getMsal();
    const activeAccount = msal.getActiveAccount() ?? msal.getAllAccounts()[0] ?? null;

    if (activeAccount) {
      msal.setActiveAccount(activeAccount);
    }

    this.account.set(activeAccount);
  }

  private setActiveAccount(result: AuthenticationResult): void {
    if (result.account) {
      this.getMsal().setActiveAccount(result.account);
      this.account.set(result.account);
    }
  }

  private getLoginScopes(): string[] {
    return [...new Set([...this.azureAd.loginScopes, ...this.getApiScopes()])];
  }

  private getApiScopes(): string[] {
    return (this.azureAd.apiScopes ?? []).filter(scope => this.isValueConfigured(scope));
  }

  private resolveRedirectUri(): string {
    if (this.isValueConfigured(this.azureAd.redirectUri)) {
      return this.azureAd.redirectUri;
    }

    return this.browser ? `${window.location.origin}/login` : '/login';
  }

  private resolvePostLogoutRedirectUri(): string {
    if (this.isValueConfigured(this.azureAd.postLogoutRedirectUri)) {
      return this.azureAd.postLogoutRedirectUri;
    }

    return this.browser ? `${window.location.origin}/login` : '/login';
  }

  private assertConfigured(): void {
    if (!this.isConfigured()) {
      throw new Error('Azure AD is not configured. Update src/environments/environment.ts with your tenant, client, and API scope values.');
    }
  }

  private getLocalToken(): string | null {
    if (!this.browser) {
      return null;
    }

    return localStorage.getItem(TOKEN_KEY);
  }

  private getStoredUser(): string | null {
    if (!this.browser) {
      return null;
    }

    return localStorage.getItem(USER_KEY);
  }

  private hasLocalToken(): boolean {
    return !!this.getLocalToken();
  }

  private setLocalSession(token: string, email: string): void {
    if (this.browser) {
      localStorage.setItem(TOKEN_KEY, token);
      localStorage.setItem(USER_KEY, email);
    }

    this.localUser.set(email);
    this.localAuthenticated.set(true);
  }

  private clearLocalSession(): void {
    if (this.browser) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    }

    this.localUser.set(null);
    this.localAuthenticated.set(false);
  }

  private isValueConfigured(value: string | undefined | null): boolean {
    return !!value && !value.startsWith('YOUR_');
  }
}
