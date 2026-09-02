import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { AnalyticsDataService, TableColumn, fmt } from '../core/analytics-data.service';
import { lineD, linePts } from '../core/chart';
import { DataTable } from '../ui/data-table';
import { InlineMessage } from '../ui/inline-message';
import { PageState } from '../ui/page-state';
import { Tabs } from '../ui/tabs';

const CHANNEL_STYLE: Record<string, { color: string; dash: string }> = {
  Direct: { color: 'var(--color-accent)', dash: '0' },
  Search: { color: 'var(--color-info)', dash: '6 3' },
  Referral: { color: 'var(--color-warning)', dash: '2 3' },
  Social: { color: 'var(--color-text-secondary)', dash: '10 4' },
};

const GROUP_COLS: Record<string, TableColumn[]> = {
  referrers: [
    { key: 'name', label: 'Referrer' },
    { key: 'v', label: 'Visitors', numeric: true },
    { key: 'share', label: 'Share', numeric: true },
  ],
  search: [
    { key: 'name', label: 'Search engine' },
    { key: 'v', label: 'Visitors', numeric: true },
    { key: 'share', label: 'Share', numeric: true },
  ],
  social: [
    { key: 'name', label: 'Platform' },
    { key: 'v', label: 'Visitors', numeric: true },
    { key: 'share', label: 'Share', numeric: true },
  ],
  campaigns: [
    { key: 'name', label: 'Campaign' },
    { key: 'v', label: 'Visitors', numeric: true },
    { key: 'share', label: 'Share', numeric: true },
  ],
};

@Component({
  selector: 'mo-sources',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DataTable, InlineMessage, PageState, Tabs],
  template: `
    <section>
      <h1 class="mo-page-title" style="margin:4px 0 16px">Traffic sources</h1>
      @if (state() !== 'ready') {
        <mo-page-state [kind]="state()" />
      } @else if (!data.channelRows().length) {
        <mo-page-state kind="empty" />
      } @else {
        <div class="mo-card" style="padding:16px 18px 10px;margin-bottom:16px">
          <div style="display:flex;align-items:center;gap:14px;flex-wrap:wrap;margin-bottom:12px">
            <span class="mo-card-label">Traffic by channel</span>
            <div class="mo-spacer"></div>
            @for (l of legend(); track l.name) {
              <span style="display:inline-flex;align-items:center;gap:6px;font-size:12px;color:var(--color-text-secondary)">
                <svg width="18" height="6" aria-hidden="true"><line x1="0" y1="3" x2="18" y2="3" [attr.stroke]="l.color" stroke-width="2" [attr.stroke-dasharray]="l.dash" /></svg>
                {{ l.name }} · {{ l.total }}
              </span>
            }
          </div>
          <svg viewBox="0 0 760 190" style="width:100%;height:auto;display:block" role="img" aria-label="Daily visits by channel over the selected period">
            <line x1="8" y1="174" x2="752" y2="174" stroke="var(--color-border)" />
            @for (l of legend(); track l.name) {
              <path [attr.d]="l.line" fill="none" [attr.stroke]="l.color" stroke-width="2" [attr.stroke-dasharray]="l.dash" />
            }
          </svg>
          <div style="display:flex;justify-content:space-between;padding:6px 4px 4px;font-size:11px;color:var(--color-text-disabled)">
            @for (a of axisLabels(); track $index) {
              <span>{{ a }}</span>
            }
          </div>
          <div style="font-size:11px;color:var(--color-text-disabled);padding:0 4px 6px">Channel lines are scaled from period totals; exact daily channel series are coming soon.</div>
        </div>
        <mo-tabs [tabs]="tabs" [value]="data.sourceGroup()" (valueChange)="data.sourceGroup.set($event)" />
        <div class="mo-card" style="border-top:none;border-radius:0 0 6px 6px;overflow-x:auto">
          @if (tableRows().length) {
            <mo-data-table [columns]="tableCols()" [rows]="tableRows()" />
          } @else {
            <div style="padding:24px 16px;text-align:center;font-size:13px;color:var(--color-text-secondary)">Nothing in this group for the selected period.</div>
          }
        </div>
        <div style="margin-top:12px">
          <mo-inline-message tone="info">Sources are aggregated by domain. Mochi never records which visitor arrived from where.</mo-inline-message>
        </div>
      }
    </section>
  `,
})
export class Sources {
  protected readonly data = inject(AnalyticsDataService);

  protected readonly tabs = [
    { value: 'referrers', label: 'Referrers' },
    { value: 'search', label: 'Search engines' },
    { value: 'social', label: 'Social' },
    { value: 'campaigns', label: 'Campaigns & UTM' },
  ];

  protected readonly state = computed(() =>
    this.data.stateOf(this.data.channelsRes, this.data.timeseriesRes, this.data.sourceGroupRes),
  );

  protected readonly axisLabels = computed(() => this.data.axisLabels(3));

  // Approximation: each channel line is the visitors series scaled by the channel's share.
  protected readonly legend = computed(() => {
    const series = this.data.series();
    const max = Math.max(1, ...series) * 1.06;
    return this.data.channelRows().map(r => {
      const style = CHANNEL_STYLE[r.name] ?? { color: 'var(--color-text-disabled)', dash: '0' };
      const scaled = series.map(v => (v * r.pct) / 100);
      return {
        name: r.name,
        total: r.val,
        color: style.color,
        dash: style.dash,
        line: series.length > 1 ? lineD(linePts(scaled, 8, 752, 14, 174, max)) : '',
      };
    });
  });

  protected readonly tableCols = computed(() => GROUP_COLS[this.data.sourceGroup()] ?? GROUP_COLS['referrers']);
  protected readonly tableRows = computed(() =>
    (this.data.sourceGroupRes.value() ?? []).map(r => ({
      id: r.name,
      name: r.name,
      v: fmt(r.count),
      share: Math.round(r.pct) + '%',
    })),
  );
}
