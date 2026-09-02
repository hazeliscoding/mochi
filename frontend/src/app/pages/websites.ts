import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AnalyticsDataService, SiteInfo } from '../core/analytics-data.service';
import { Icon } from '../ui/icon';
import { PageState } from '../ui/page-state';
import { StatusIndicator } from '../ui/status-indicator';

@Component({
  selector: 'mo-websites',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon, PageState, StatusIndicator],
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
      @if (state() !== 'ready') {
        <mo-page-state [kind]="state()" />
      } @else if (!data.sites().length) {
        <div class="mo-card" style="padding:44px 24px;text-align:center;color:var(--color-text-secondary);font-size:13.5px">
          <span style="display:block;font-weight:600;color:var(--color-text-primary);margin-bottom:4px">No websites yet</span>
          <span>Add your first website to start collecting privacy-first analytics.</span>
        </div>
      } @else {
        <div class="mo-card">
          @for (s of data.sites(); track s.id) {
            <button type="button" class="mo-row-btn" style="display:flex;align-items:center;gap:20px;padding:16px 18px;flex-wrap:wrap" (click)="open(s)">
              <div style="min-width:220px;flex:1">
                <div style="font:600 16px var(--font-display)">{{ s.name }}</div>
                <div class="mo-mono" style="font-size:12px;color:var(--color-text-secondary);margin-top:2px">{{ s.domain }}</div>
              </div>
              <div style="min-width:120px;text-align:right">
                <div class="mo-num" style="font:600 16px var(--font-display)">{{ s.views }}</div>
                <div style="font-size:12px;color:var(--color-text-secondary)">views last 30 days</div>
              </div>
              <div style="min-width:110px;font-size:13px;color:var(--color-text-secondary)">{{ s.active }}</div>
              <mo-status [tone]="s.tone">{{ s.status }}</mo-status>
            </button>
          }
        </div>
      }
    </section>
  `,
})
export class Websites {
  protected readonly data = inject(AnalyticsDataService);
  protected readonly router = inject(Router);

  protected readonly state = computed(() => this.data.stateOf(this.data.sitesRes));

  protected open(s: SiteInfo): void {
    this.data.siteId.set(s.id);
    this.router.navigate(['/overview']);
  }
}
