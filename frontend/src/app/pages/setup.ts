import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, apiError } from '../core/auth.service';
import { InlineMessage } from '../ui/inline-message';

// Mirrors the server rule; the API rejects shorter passwords anyway.
const MIN_PASSWORD = 10;

@Component({
  selector: 'mo-setup',
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
            <div style="font:600 18px var(--font-display)">Set up Mochi</div>
            <div style="font-size:13px;color:var(--color-text-secondary);margin-top:2px">
              Create the admin account for this install.
            </div>
          </div>
          <div>
            <label class="tr-label" for="setup-code">Setup code</label>
            <input
              id="setup-code"
              type="text"
              class="tr-input mo-mono"
              autocomplete="off"
              [value]="code()"
              (input)="code.set(val($event))"
            />
            <div class="tr-hint">
              Printed in the server's console log when Mochi starts with no accounts.
            </div>
          </div>
          <div>
            <label class="tr-label" for="setup-email">Email</label>
            <input
              id="setup-email"
              type="email"
              class="tr-input"
              autocomplete="email"
              [value]="email()"
              (input)="email.set(val($event))"
            />
          </div>
          <div>
            <label class="tr-label" for="setup-password">Password</label>
            <input
              id="setup-password"
              type="password"
              class="tr-input"
              autocomplete="new-password"
              [value]="password()"
              (input)="password.set(val($event))"
            />
            <div class="tr-hint">At least {{ minPassword }} characters.</div>
          </div>
          @if (error()) {
            <mo-inline-message tone="danger">{{ error() }}</mo-inline-message>
          }
          <button type="submit" class="tr-btn tr-btn--primary" [disabled]="busy()">
            {{ busy() ? 'Creating account…' : 'Create account' }}
          </button>
        </form>
      </div>
    </div>
  `,
})
export class Setup {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly minPassword = MIN_PASSWORD;
  protected readonly code = signal('');
  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  protected val(e: Event): string {
    return (e.target as HTMLInputElement).value;
  }

  protected async submit(e: Event): Promise<void> {
    e.preventDefault();
    const code = this.code().trim();
    const email = this.email().trim();
    if (!code || !email) {
      this.error.set('Setup code and email are both required.');
      return;
    }
    if (this.password().length < MIN_PASSWORD) {
      this.error.set(`Password must be at least ${MIN_PASSWORD} characters.`);
      return;
    }
    this.busy.set(true);
    this.error.set('');
    try {
      await this.auth.setup(code, email, this.password());
      this.router.navigateByUrl('/');
    } catch (err) {
      this.error.set(apiError(err, 'Could not complete setup. Try again.'));
    } finally {
      this.busy.set(false);
    }
  }
}
