import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AnalyticsDataService, TableColumn, fmt } from '../core/analytics-data.service';
import { areaD, lineD, linePts } from '../core/chart';
import { ButtonGroup } from '../ui/button-group';
import { DataTable } from '../ui/data-table';
import { Metric } from '../ui/metric';
import { StatusIndicator } from '../ui/status-indicator';

@Component({
  selector: 'mo-overview',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Metric, StatusIndicator, ButtonGroup, DataTable],
  template: `
    <section>
      <div style="display:flex;align-items:flex-start;justify-content:space-between;gap:16px;flex-wrap:wrap;margin:4px 0 20px">
        <div>
          <h1 class="mo-page-title">{{ data.site() }}</h1>
          <div style="font-size:13px;color:var(--color-text-secondary);margin-top:5px">Aug 1 – Aug 30, 2026 · {{ compareLabel() }}</div>
        </div>
        <mo-status tone="success">Receiving data</mo-status>
      </div>

      <div class="mo-card mo-metric-strip" style="margin-bottom:16px">
        @for (m of data.metrics; track m.label) {
          <div [title]="m.tip">
            <mo-metric [label]="m.label" [value]="m.value" [delta]="m.delta" [dir]="m.dir" />
          </div>
        }
      </div>

      <div class="mo-card" style="padding:16px 18px 10px;margin-bottom:16px">
        <div style="display:flex;align-items:center;gap:16px;flex-wrap:wrap;margin-bottom:12px">
          <span class="mo-card-label">Trend</span>
          <div class="mo-spacer"></div>
          <span style="display:inline-flex;align-items:center;gap:6px;font-size:12px;color:var(--color-text-secondary)"><span style="width:16px;height:0;border-top:2px solid var(--color-accent)"></span>This period</span>
          <span style="display:inline-flex;align-items:center;gap:6px;font-size:12px;color:var(--color-text-secondary)"><span style="width:16px;height:0;border-top:2px dashed var(--color-text-disabled)"></span>Previous</span>
          <mo-button-group [items]="metricItems" [value]="metric()" (valueChange)="setMetric($event)" />
        </div>
        <svg viewBox="0 0 760 230" style="width:100%;height:auto;display:block" role="img" [attr.aria-label]="chartAria()">
          <line x1="8" y1="8" x2="752" y2="8" stroke="var(--color-border-subtle)" />
          <line x1="8" y1="111" x2="752" y2="111" stroke="var(--color-border-subtle)" />
          <line x1="8" y1="214" x2="752" y2="214" stroke="var(--color-border)" />
          <path [attr.d]="chart().area" fill="var(--color-accent-subtle)" opacity="0.65" />
          <path [attr.d]="chart().prev" fill="none" stroke="var(--color-text-disabled)" stroke-width="1.5" stroke-dasharray="4 4" />
          <path [attr.d]="chart().line" fill="none" stroke="var(--color-accent)" stroke-width="2" stroke-linejoin="round" />
          <line [attr.x1]="chart().spikeX" y1="20" [attr.x2]="chart().spikeX" [attr.y2]="chart().spikeY" stroke="var(--color-border)" stroke-dasharray="2 3" />
          <circle [attr.cx]="chart().spikeX" [attr.cy]="chart().spikeY" r="4" fill="var(--color-surface)" stroke="var(--color-accent)" stroke-width="2">
            <title>Aug 19 — Front page of Hacker News</title>
          </circle>
          <text [attr.x]="chart().spikeX" y="14" text-anchor="middle" style="font:11px var(--font-ui);fill:var(--color-text-secondary)">Hacker News front page</text>
          <text x="10" y="22" style="font:11px var(--font-ui);fill:var(--color-text-disabled)">{{ chart().yMax }}</text>
          <text x="10" y="125" style="font:11px var(--font-ui);fill:var(--color-text-disabled)">{{ chart().yMid }}</text>
        </svg>
        <div style="display:flex;justify-content:space-between;padding:6px 4px 4px;font-size:11px;color:var(--color-text-disabled)">
          <span>Aug 1</span><span>Aug 8</span><span>Aug 15</span><span>Aug 22</span><span>Aug 30</span>
        </div>
      </div>

      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:16px">
        <div class="mo-card" style="display:flex;flex-direction:column">
          <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 16px 8px">
            <span class="mo-card-label">Top pages</span>
            <button type="button" class="tr-btn tr-btn--ghost tr-btn--sm" (click)="go('/pages')">All pages</button>
          </div>
          <mo-data-table [columns]="miniPageCols" [rows]="miniPageRows" [clickable]="true" (rowClick)="openPage($event)" />
        </div>
        <div class="mo-card">
          <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 16px 8px">
            <span class="mo-card-label">Traffic sources</span>
            <button type="button" class="tr-btn tr-btn--ghost tr-btn--sm" (click)="go('/sources')">Sources</button>
          </div>
          <div style="padding:2px 8px 8px">
            @for (r of data.channelRows; track r.name) {
              <div class="mo-bar-row">
                <div class="mo-bar-row__fill" [style.width.%]="r.pct"></div>
                <span class="mo-bar-row__name">{{ r.name }}</span>
                <span class="mo-bar-row__vals"><span class="mo-bar-row__pct">{{ r.pct }}%</span><span class="mo-bar-row__val">{{ r.val }}</span></span>
              </div>
            }
            <div style="height:1px;background:var(--color-border-subtle);margin:8px 2px"></div>
            <div class="mo-card-label" style="padding:4px 10px 6px">Top sources</div>
            @for (r of data.topSourceRows; track r.name) {
              <div class="mo-kv-row"><span>{{ r.name }}</span><span>{{ r.val }}</span></div>
            }
          </div>
        </div>
        <div class="mo-card">
          <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 16px 8px">
            <span class="mo-card-label">Visitor geography</span>
            <button type="button" class="tr-btn tr-btn--ghost tr-btn--sm" (click)="go('/geography')">Geography</button>
          </div>
          <div style="padding:2px 8px 8px">
            @for (r of countryRows; track r.name) {
              <div class="mo-bar-row">
                <div class="mo-bar-row__fill" [style.width.%]="r.pct"></div>
                <span class="mo-bar-row__name">{{ r.name }}</span>
                <span class="mo-bar-row__vals"><span class="mo-bar-row__pct">{{ r.pct }}%</span><span class="mo-bar-row__val">{{ r.val }}</span></span>
              </div>
            }
            <div style="padding:8px 10px 4px;font-size:12px;color:var(--color-text-secondary)">Locations are generalized to country level.</div>
          </div>
        </div>
        <div class="mo-card">
          <div class="mo-card-label" style="padding:14px 16px 10px">Devices</div>
          <div style="padding:0 16px 14px">
            <div class="mo-split" aria-hidden="true">
              <div style="width:58%;background:var(--color-accent)"></div>
              <div style="width:36%;background:var(--teal-300)"></div>
              <div style="width:6%;background:var(--color-border)"></div>
            </div>
            <div style="display:flex;gap:18px;margin-top:10px;font-size:13px;flex-wrap:wrap">
              <span><span class="mo-dot" style="background:var(--color-accent)"></span>Desktop 58%</span>
              <span><span class="mo-dot" style="background:var(--teal-300)"></span>Mobile 36%</span>
              <span><span class="mo-dot" style="background:var(--color-border)"></span>Tablet 6%</span>
            </div>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:0 20px;margin-top:16px">
              <div>
                <div class="mo-card-label" style="padding-bottom:6px">Browsers</div>
                @for (r of data.browserTop; track r.name) {
                  <div style="display:flex;justify-content:space-between;padding:4px 0;font-size:13px"><span>{{ r.name }}</span><span class="mo-muted mo-num">{{ r.pct }}%</span></div>
                }
              </div>
              <div>
                <div class="mo-card-label" style="padding-bottom:6px">Operating systems</div>
                @for (r of data.osTop; track r.name) {
                  <div style="display:flex;justify-content:space-between;padding:4px 0;font-size:13px"><span>{{ r.name }}</span><span class="mo-muted mo-num">{{ r.pct }}%</span></div>
                }
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  `,
})
export class Overview {
  protected readonly data = inject(AnalyticsDataService);
  private readonly router = inject(Router);

  protected readonly metric = signal<'visitors' | 'pageviews' | 'sessions'>('visitors');
  protected readonly metricItems = [
    { value: 'visitors', label: 'Visitors' },
    { value: 'pageviews', label: 'Pageviews' },
    { value: 'sessions', label: 'Sessions' },
  ];

  protected readonly compareLabel = computed(() =>
    this.data.compare() === 'No comparison' ? 'no comparison' : this.data.compare(),
  );

  protected readonly chartAria = computed(
    () => 'Daily ' + this.metric() + ' over the last 30 days, trending up with a spike on August 19',
  );

  protected readonly chart = computed(() => {
    const m = this.metric();
    const series = m === 'pageviews' ? this.data.pageviews : m === 'sessions' ? this.data.sessions : this.data.visitors;
    const scale = m === 'pageviews' ? 1.85 : m === 'sessions' ? 1.1 : 1;
    const prevS = this.data.prev.map(v => Math.round(v * scale));
    const yMaxRaw = Math.max(...series, ...prevS) * 1.06;
    const pts = linePts(series, 8, 752, 24, 214, yMaxRaw);
    const ptsPrev = linePts(prevS, 8, 752, 24, 214, yMaxRaw);
    return {
      line: lineD(pts),
      area: areaD(pts, 214),
      prev: lineD(ptsPrev),
      spikeX: pts[18][0].toFixed(1),
      spikeY: pts[18][1].toFixed(1),
      yMax: fmt(Math.round(yMaxRaw)),
      yMid: fmt(Math.round(yMaxRaw / 2)),
    };
  });

  protected readonly miniPageCols: TableColumn[] = [
    { key: 'page', label: 'Page' },
    { key: 'v', label: 'Visitors', numeric: true },
    { key: 'pv', label: 'Pageviews', numeric: true },
    { key: 'bounce', label: 'Bounce', numeric: true },
  ];

  protected readonly miniPageRows = this.data.pages.slice(0, 5).map(p => ({
    id: p.id,
    page: p.id,
    v: fmt(p.v),
    pv: fmt(p.pv),
    bounce: p.bounce + '%',
  }));

  protected readonly countryRows = this.data.geo.slice(0, 7).map(g => ({ name: g[0], val: fmt(g[1]), pct: g[2] }));

  protected setMetric(v: string): void {
    this.metric.set(v as 'visitors' | 'pageviews' | 'sessions');
  }

  protected go(path: string): void {
    this.router.navigate([path]);
  }

  protected openPage(row: Record<string, string>): void {
    this.router.navigate(['/pages'], { queryParams: { path: row['id'] } });
  }
}
