import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AnalyticsDataService, fmt } from '../core/analytics-data.service';
import { BarList } from '../ui/bar-list';
import { InlineMessage } from '../ui/inline-message';

interface MapDot {
  cx: number;
  cy: number;
  fill: string;
  opacity: number;
}

@Component({
  selector: 'mo-geography',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BarList, InlineMessage],
  template: `
    <section>
      <h1 class="mo-page-title" style="margin:4px 0 16px">Geography</h1>
      <div style="margin-bottom:16px">
        <mo-inline-message tone="info">Locations are intentionally generalized to protect visitor privacy.</mo-inline-message>
      </div>
      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:16px">
        <div class="mo-card" style="padding:16px 18px">
          <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px">
            <span class="mo-card-label">Visits by country</span>
            <span style="display:inline-flex;align-items:center;gap:5px;font-size:11px;color:var(--color-text-secondary)">
              Fewer
              <span style="width:8px;height:8px;border-radius:50%;background:var(--color-accent);opacity:.3"></span>
              <span style="width:8px;height:8px;border-radius:50%;background:var(--color-accent);opacity:.6"></span>
              <span style="width:8px;height:8px;border-radius:50%;background:var(--color-accent)"></span>
              More
            </span>
          </div>
          <svg viewBox="0 0 600 235" style="width:100%;height:auto;display:block" role="img" aria-label="Stylized world map; visits concentrated in the United States, Germany, the United Kingdom and Japan">
            @for (d of dots; track $index) {
              <circle [attr.cx]="d.cx" [attr.cy]="d.cy" r="2.7" [attr.fill]="d.fill" [attr.opacity]="d.opacity" />
            }
          </svg>
          <div style="font-size:12px;color:var(--color-text-secondary);margin-top:8px">Country-level only. No cities, no coordinates, no IP addresses stored.</div>
        </div>
        <div class="mo-card">
          <div class="mo-card-label" style="padding:14px 16px 6px">Countries</div>
          <div style="padding:0 8px 8px">
            <mo-bar-list [rows]="geoRows" [valWidth]="48" />
            <div style="padding:8px 10px;font-size:12px;color:var(--color-text-secondary)">Countries with fewer than 25 visits this period are grouped into “Other”.</div>
          </div>
        </div>
      </div>
      <div class="mo-card" style="margin-top:16px;padding:14px 16px">
        <div class="mo-card-label" style="margin-bottom:8px">Regions · United States</div>
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:4px 24px">
          @for (r of data.usRegions; track r.name) {
            <div style="display:flex;justify-content:space-between;padding:5px 0;font-size:13px;border-bottom:1px solid var(--color-border-subtle)"><span>{{ r.name }}</span><span class="mo-muted mo-num">{{ r.val }}</span></div>
          }
        </div>
        <div style="font-size:12px;color:var(--color-text-secondary);margin-top:10px">Regions with too few visits to be safely aggregated are hidden.</div>
      </div>
    </section>
  `,
})
export class Geography {
  protected readonly data = inject(AnalyticsDataService);

  protected readonly geoRows = this.data.geo.map(g => ({ name: g[0], val: fmt(g[1]), pct: g[2] }));

  protected readonly dots: MapDot[] = this.buildDots();

  private buildDots(): MapDot[] {
    const hlAt = (r: number, c: number): number => {
      for (const h of this.data.mapHighlights) {
        if (r >= h[0] && r <= h[1] && c >= h[2] && c <= h[3]) return h[4];
      }
      return 0;
    };
    const dots: MapDot[] = [];
    this.data.mapLand.forEach((ranges, r) =>
      ranges.forEach(rg => {
        for (let c = rg[0]; c <= rg[1]; c++) {
          const o = hlAt(r, c);
          dots.push({
            cx: 6 + c * 9.9,
            cy: 8 + r * 9.3,
            fill: o ? 'var(--color-accent)' : 'var(--color-border)',
            opacity: o ? 0.35 + o * 0.65 : 0.75,
          });
        }
      }),
    );
    return dots;
  }
}
