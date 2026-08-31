import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AnalyticsDataService } from '../core/analytics-data.service';
import { CodeBlock } from '../ui/code-block';
import { Tabs } from '../ui/tabs';

@Component({
  selector: 'mo-add-website',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CodeBlock, Tabs],
  template: `
    <section style="max-width:640px;margin:0 auto">
      <h1 class="mo-page-title" style="margin:4px 0 6px">Add a website</h1>
      <div style="font-size:14px;color:var(--color-text-secondary);margin-bottom:20px">Three small steps. No cookie banner required afterwards.</div>
      <div style="display:flex;gap:8px;margin-bottom:24px" aria-hidden="true">
        @for (n of [1, 2, 3]; track n) {
          <div style="flex:1;display:flex;align-items:center;gap:8px">
            <span
              style="width:22px;height:22px;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;font-size:12px;font-weight:600"
              [style.background]="step() >= n ? 'var(--color-accent)' : 'var(--color-surface)'"
              [style.color]="step() >= n ? 'var(--color-on-accent)' : 'var(--color-text-secondary)'"
              [style.border]="'1px solid ' + (step() >= n ? 'var(--color-accent)' : 'var(--color-border)')"
            >{{ step() > n ? '✓' : n }}</span>
            <span style="font-size:13px" [style.color]="step() === n ? 'var(--color-text-primary)' : 'var(--color-text-secondary)'">{{ stepLabels[n - 1] }}</span>
            <span style="flex:1;height:1px;background:var(--color-border-subtle)"></span>
          </div>
        }
      </div>

      @switch (step()) {
        @case (1) {
          <div class="mo-card" style="padding:22px;display:flex;flex-direction:column;gap:16px">
            <div>
              <label class="tr-label" for="site-name">Website name</label>
              <input id="site-name" type="text" class="tr-input" placeholder="My portfolio" />
            </div>
            <div>
              <label class="tr-label" for="site-domain">Domain</label>
              <input id="site-domain" type="text" class="tr-input" placeholder="example.com" />
              <div class="tr-hint">Just the domain — no https:// or paths.</div>
            </div>
            <div>
              <label class="tr-label" for="site-tz">Time zone</label>
              <select id="site-tz" class="tr-select" (change)="onTz($event)">
                @for (t of data.tzOptions; track t) {
                  <option [value]="t" [selected]="t === tz()">{{ t }}</option>
                }
              </select>
            </div>
            <div style="display:flex;justify-content:flex-end">
              <button type="button" class="tr-btn tr-btn--primary" (click)="step.set(2)">Continue</button>
            </div>
          </div>
        }
        @case (2) {
          <div class="mo-card" style="padding:22px">
            <div style="font-weight:600;margin-bottom:4px">Install the snippet</div>
            <div style="font-size:13px;color:var(--color-text-secondary);margin-bottom:14px">One script tag, under 1 KB, no cookies. Pick your setup:</div>
            <mo-tabs [tabs]="fwTabs" [value]="fw()" (valueChange)="fw.set($event)" />
            <div style="margin-top:14px">
              <mo-code-block [filename]="fwEntry()[0]" [code]="fwEntry()[1]" />
            </div>
            <div style="display:flex;justify-content:space-between;margin-top:18px">
              <button type="button" class="tr-btn tr-btn--ghost" (click)="step.set(1)">Back</button>
              <button type="button" class="tr-btn tr-btn--primary" (click)="step.set(3)">I've installed it</button>
            </div>
          </div>
        }
        @case (3) {
          <div class="mo-card" style="padding:40px 24px;text-align:center">
            <div style="width:56px;height:56px;border-radius:50%;background:var(--color-success-bg);display:inline-flex;align-items:center;justify-content:center;animation:mo-pop .5s ease-out both">
              <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="var(--color-success)" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5" /></svg>
            </div>
            <div style="font:600 20px var(--font-display);margin-top:14px">Mochi is receiving analytics from your website. 🍡</div>
            <div style="font-size:14px;color:var(--color-text-secondary);margin-top:6px;max-width:400px;margin-left:auto;margin-right:auto">Your first visit just arrived. Aggregates will build up over the next few hours.</div>
            <div style="margin-top:20px;display:flex;gap:8px;justify-content:center">
              <button type="button" class="tr-btn tr-btn--ghost" (click)="step.set(2)">Back</button>
              <button type="button" class="tr-btn tr-btn--primary" (click)="router.navigate(['/overview'])">Go to dashboard</button>
            </div>
          </div>
        }
      }
    </section>
  `,
})
export class AddWebsite {
  protected readonly data = inject(AnalyticsDataService);
  protected readonly router = inject(Router);

  protected readonly step = signal(1);
  protected readonly stepLabels = ['Website details', 'Install snippet', 'Verify'];
  protected readonly tz = signal('(UTC−05:00) Eastern Time');

  protected readonly fw = signal('HTML');
  protected readonly fwTabs = Object.keys(this.data.frameworks).map(k => ({ value: k, label: k }));
  protected readonly fwEntry = computed(() => this.data.frameworks[this.fw()]);

  protected onTz(e: Event): void {
    this.tz.set((e.target as HTMLSelectElement).value);
  }
}
