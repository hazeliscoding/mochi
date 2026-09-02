import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, apiError } from '../core/auth.service';
import { InlineMessage } from '../ui/inline-message';

@Component({
  selector: 'mo-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [InlineMessage],
  template: `
    <div class="mo-auth">
      <div class="mo-auth__card">
        <div
          style="display:flex;align-items:center;gap:8px;justify-content:center;margin-bottom:18px"
        >
          <svg width="22" height="22" viewBox="0 0 20 20" aria-hidden="true">
            <circle
              cx="10"
              cy="10"
              r="8.5"
              fill="var(--color-accent-subtle)"
              stroke="var(--color-accent)"
              stroke-width="1.5"
            />
            <circle cx="10" cy="10" r="3.2" fill="var(--color-accent)" />
          </svg>
          <span class="mo-brand" style="font:600 19px var(--font-display)">Mochi</span>
        </div>
        <form
          class="mo-card"
          style="padding:22px;display:flex;flex-direction:column;gap:14px"
          (submit)="submit($event)"
        >
          <div>
            <div style="font:600 18px var(--font-display)">Sign in</div>
            <div style="font-size:13px;color:var(--color-text-secondary);margin-top:2px">
              Privacy-first analytics for your websites.
            </div>
          </div>
          <div>
            <label class="tr-label" for="login-email">Email</label>
            <input
              id="login-email"
              type="email"
              class="tr-input"
              autocomplete="email"
              [value]="email()"
              (input)="email.set(val($event))"
            />
          </div>
          <div>
            <label class="tr-label" for="login-password">Password</label>
            <input
              id="login-password"
              type="password"
              class="tr-input"
              autocomplete="current-password"
              [value]="password()"
              (input)="password.set(val($event))"
            />
          </div>
          @if (error()) {
            <mo-inline-message tone="danger">{{ error() }}</mo-inline-message>
          }
          <button type="submit" class="tr-btn tr-btn--primary" [disabled]="busy()">
            {{ busy() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
      </div>
    </div>
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  protected val(e: Event): string {
    return (e.target as HTMLInputElement).value;
  }

  protected async submit(e: Event): Promise<void> {
    e.preventDefault();
    const email = this.email().trim();
    if (!email || !this.password()) {
      this.error.set('Email and password are both required.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    try {
      await this.auth.login(email, this.password());
      this.router.navigateByUrl('/');
    } catch (err) {
      this.error.set(apiError(err, 'Could not sign in. Try again.'));
    } finally {
      this.busy.set(false);
    }
  }
}
