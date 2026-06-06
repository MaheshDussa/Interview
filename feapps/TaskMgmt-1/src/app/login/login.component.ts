import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AppInsightsService } from '../core/app-insights.service';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private appInsights = inject(AppInsightsService);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal<string | null>(null);
  readonly isConfigured = computed(() => this.auth.isConfigured());
  readonly usesAzureAd = computed(() => this.auth.usesAzureAd());
  readonly userDisplayName = this.auth.userDisplayName;

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  ngOnInit(): void {
    if (this.auth.isAuthenticated()) {
      this.router.navigate(['/tasks']);
    }
  }

  async signInWithAzureAd(): Promise<void> {
    if (this.loading()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      await this.auth.loginWithAzureAd();
      await this.router.navigate(['/tasks']);
    } catch (error: any) {
      this.appInsights.trackCodeError(error, { source: 'login.azureAd' });
      this.error.set(error?.message || 'Azure AD sign-in failed. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  submitLocalLogin(): void {
    if (this.loading()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.loginWithEmail(this.form.getRawValue()).subscribe({
      next: async () => {
        this.loading.set(false);
        await this.router.navigate(['/tasks']);
      },
      error: err => {
        this.appInsights.trackCodeError(err, { source: 'login.local' });
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Login failed. Please try again.');
      }
    });
  }
}
