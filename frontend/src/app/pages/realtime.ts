import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AnalyticsDataService } from '../core/analytics-data.service';
import { BarList } from '../ui/bar-list';

@Component({
  selector: 'mo-realtime',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BarList],
  template: `
    <section>
      <h1 class="mo-page-title" style="margin:4px 0 20px">Realtime</h1>
      <div class="mo-card" style="padding:22px 24px;margin-bottom:16px;display:flex;align-items:center;gap:16px;flex-wrap:wrap">
        <span style="width:10px;height:10px;border-radius:50%;background:var(--color-accent);flex:none;animation:mo-pulse 2.6s ease-out infinite"></span>
        <span class="mo-num" style="font:600 40px/1 var(--font-display)">18</span>
        <span style="font-size:16px">active visits in the last 5 minutes</span>
        <span class="mo-spacer"></span>
        <span style="font-size:12px;color:var(--color-text-secondary)">Aggregates only — never individual profiles</span>
      </div>
      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:16px">
        <div class="mo-card" style="padding:14px 16px">
          <div class="mo-card-label" style="margin-bottom:10px">Pageviews · last 30 minutes</div>
          <svg viewBox="0 0 300 72" style="width:100%;height:auto;display:block" role="img" aria-label="Pageviews per minute over the last 30 minutes, between 0 and 9 per minute">
            @for (v of data.rtVals; track $index) {
              <rect [attr.x]="$index * 10" width="7" [attr.y]="68 - v * 7" [attr.height]="v * 7" rx="1.5" fill="var(--color-accent)" [attr.opacity]="$index === 29 ? 1 : 0.45 + ($index / 29) * 0.4" />
            }
          </svg>
          <div style="display:flex;justify-content:space-between;font-size:11px;color:var(--color-text-disabled);margin-top:4px"><span>30 min ago</span><span>now</span></div>
        </div>
        <div class="mo-card" style="padding:14px 8px">
          <div class="mo-card-label" style="margin:0 8px 8px">Active pages</div>
          <mo-bar-list [rows]="data.rtPages" [showPct]="false" [mono]="true" />
        </div>
        <div class="mo-card" style="padding:14px 8px">
          <div class="mo-card-label" style="margin:0 8px 8px">Incoming sources</div>
          <mo-bar-list [rows]="data.rtSources" [showPct]="false" />
        </div>
        <div class="mo-card" style="padding:14px 16px">
          <div class="mo-card-label" style="margin-bottom:8px">Countries</div>
          @for (r of data.rtCountries; track r.name) {
            <div style="display:flex;justify-content:space-between;padding:5px 0;font-size:13px"><span>{{ r.name }}</span><span class="mo-muted mo-num">{{ r.val }}</span></div>
          }
          <div style="height:1px;background:var(--color-border-subtle);margin:10px 0"></div>
          <div class="mo-card-label" style="margin-bottom:8px">Devices</div>
          <div class="mo-split" aria-hidden="true">
            <div style="width:61%;background:var(--color-accent)"></div>
            <div style="width:39%;background:var(--teal-300)"></div>
          </div>
          <div style="display:flex;gap:16px;margin-top:8px;font-size:13px"><span>Desktop 11</span><span class="mo-muted">Mobile 7</span></div>
        </div>
      </div>
    </section>
  `,
})
export class Realtime {
  protected readonly data = inject(AnalyticsDataService);
}
