import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AnalyticsDataService } from '../core/analytics-data.service';
import { lineD, linePts } from '../core/chart';
import { DataTable } from '../ui/data-table';
import { InlineMessage } from '../ui/inline-message';
import { Tabs } from '../ui/tabs';

@Component({
  selector: 'mo-sources',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DataTable, InlineMessage, Tabs],
  template: `
    <section>
      <h1 class="mo-page-title" style="margin:4px 0 16px">Traffic sources</h1>
      <div class="mo-card" style="padding:16px 18px 10px;margin-bottom:16px">
        <div style="display:flex;align-items:center;gap:14px;flex-wrap:wrap;margin-bottom:12px">
          <span class="mo-card-label">Traffic by channel · last 30 days</span>
          <div class="mo-spacer"></div>
          @for (l of legend; track l.name) {
            <span style="display:inline-flex;align-items:center;gap:6px;font-size:12px;color:var(--color-text-secondary)">
              <svg width="18" height="6" aria-hidden="true"><line x1="0" y1="3" x2="18" y2="3" [attr.stroke]="l.color" stroke-width="2" [attr.stroke-dasharray]="l.dash" /></svg>
              {{ l.name }} · {{ l.total }}
            </span>
          }
        </div>
        <svg viewBox="0 0 760 190" style="width:100%;height:auto;display:block" role="img" aria-label="Daily visits by channel; Direct is highest, then Search, Referral and Social">
          <line x1="8" y1="174" x2="752" y2="174" stroke="var(--color-border)" />
          <path [attr.d]="lines.direct" fill="none" stroke="var(--color-accent)" stroke-width="2" />
          <path [attr.d]="lines.search" fill="none" stroke="var(--color-info)" stroke-width="2" stroke-dasharray="6 3" />
          <path [attr.d]="lines.referral" fill="none" stroke="var(--color-warning)" stroke-width="2" stroke-dasharray="2 3" />
          <path [attr.d]="lines.social" fill="none" stroke="var(--color-text-secondary)" stroke-width="2" stroke-dasharray="10 4" />
        </svg>
        <div style="display:flex;justify-content:space-between;padding:6px 4px 4px;font-size:11px;color:var(--color-text-disabled)"><span>Aug 1</span><span>Aug 15</span><span>Aug 30</span></div>
      </div>
      <mo-tabs [tabs]="tabs" [value]="tab()" (valueChange)="tab.set($event)" />
      <div class="mo-card" style="border-top:none;border-radius:0 0 6px 6px;overflow-x:auto">
        <mo-data-table [columns]="table().cols" [rows]="table().rows" />
      </div>
      <div style="margin-top:12px">
        <mo-inline-message tone="info">Sources are aggregated by domain. Mochi never records which visitor arrived from where.</mo-inline-message>
      </div>
    </section>
  `,
})
export class Sources {
  protected readonly data = inject(AnalyticsDataService);

  protected readonly tab = signal('referrers');
  protected readonly tabs = [
    { value: 'referrers', label: 'Referrers', count: 6 },
    { value: 'search', label: 'Search engines', count: 3 },
    { value: 'social', label: 'Social', count: 3 },
    { value: 'campaigns', label: 'Campaigns & UTM', count: 3 },
  ];

  protected readonly legend = [
    { name: 'Direct', total: '3,121', color: 'var(--color-accent)', dash: '0' },
    { name: 'Search', total: '2,383', color: 'var(--color-info)', dash: '6 3' },
    { name: 'Referral', total: '1,972', color: 'var(--color-warning)', dash: '2 3' },
    { name: 'Social', total: '738', color: 'var(--color-text-secondary)', dash: '10 4' },
  ];

  protected readonly lines = {
    direct: this.chanLine(this.data.channelShares.direct, 3),
    search: this.chanLine(this.data.channelShares.search, 5),
    referral: this.chanLine(this.data.channelShares.referral, 7),
    social: this.chanLine(this.data.channelShares.social, 11),
  };

  protected readonly table = computed(() => this.data.sourceTables[this.tab()]);

  private chanLine(f: number, jseed: number): string {
    const arr = this.data.visitors.map((v, i) => Math.round(v * f * (0.82 + ((i * jseed) % 7) / 18)));
    const m = Math.max(...this.data.visitors) * 0.45;
    return lineD(linePts(arr, 8, 752, 14, 174, m));
  }
}
