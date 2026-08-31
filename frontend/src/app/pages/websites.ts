import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AnalyticsDataService, SiteInfo } from '../core/analytics-data.service';
import { sparkD } from '../core/chart';
import { Icon } from '../ui/icon';
import { Sparkline } from '../ui/sparkline';
import { StatusIndicator } from '../ui/status-indicator';

@Component({
  selector: 'mo-websites',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon, Sparkline, StatusIndicator],
  template: `
    <section>
      <div class="mo-page-head">
        <h1 class="mo-page-title">Websites</h1>
        <div class="mo-spacer"></div>
        <button type="button" class="tr-btn tr-btn--primary" (click)="router.navigate(['/add-website'])">
          <mo-icon name="plus" [size]="14" />
          Add website
        </button>
      </div>
      <div class="mo-card">
        @for (s of data.sites; track s.domain) {
          <button type="button" class="mo-row-btn" style="display:flex;align-items:center;gap:20px;padding:16px 18px;flex-wrap:wrap" (click)="open(s)">
            <div style="min-width:220px;flex:1">
              <div style="font:600 16px var(--font-display)">{{ s.name }}</div>
              <div class="mo-mono" style="font-size:12px;color:var(--color-text-secondary);margin-top:2px">{{ s.domain }}</div>
            </div>
            <mo-sparkline [path]="spark(s)" [width]="140" [height]="32" />
            <div style="min-width:120px;text-align:right">
              <div class="mo-num" style="font:600 16px var(--font-display)">{{ s.views }}</div>
              <div style="font-size:12px;color:var(--color-text-secondary)">views this month</div>
            </div>
            <div style="min-width:110px;font-size:13px;color:var(--color-text-secondary)">{{ s.active }}</div>
            <mo-status [tone]="s.tone">{{ s.status }}</mo-status>
          </button>
        }
      </div>
    </section>
  `,
})
export class Websites {
  protected readonly data = inject(AnalyticsDataService);
  protected readonly router = inject(Router);

  protected spark(s: SiteInfo): string {
    if (s.f === 0) return 'M2 29L138 29';
    return sparkD(this.data.visitors.filter((_, i) => i % 2 === 0).map(v => v * s.f), 140, 32);
  }

  protected open(s: SiteInfo): void {
    if (s.f > 0) {
      this.data.site.set(s.domain);
      this.router.navigate(['/overview']);
    } else {
      this.router.navigate(['/add-website']);
    }
  }
}
