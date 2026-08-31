import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AnalyticsDataService } from '../core/analytics-data.service';
import { BarList } from '../ui/bar-list';

@Component({
  selector: 'mo-devices',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BarList],
  template: `
    <section>
      <h1 class="mo-page-title" style="margin:4px 0 16px">Devices</h1>
      <div class="mo-card" style="padding:16px 18px;margin-bottom:16px">
        <div class="mo-card-label" style="margin-bottom:12px">Device types</div>
        <div class="mo-split mo-split--lg" aria-hidden="true">
          <div style="width:58%;background:var(--color-accent)"></div>
          <div style="width:36%;background:var(--teal-300)"></div>
          <div style="width:6%;background:var(--color-border)"></div>
        </div>
        <div style="display:flex;gap:28px;margin-top:12px;flex-wrap:wrap">
          @for (d of data.deviceRows; track d.name) {
            <div>
              <div style="font-size:13px;color:var(--color-text-secondary)"><span class="mo-dot" [style.background]="d.color"></span>{{ d.name }}</div>
              <div class="mo-num" style="font:600 20px var(--font-display);margin-top:2px">{{ d.pct }}% <span style="font:400 13px var(--font-ui);color:var(--color-text-secondary)">{{ d.val }}</span></div>
            </div>
          }
        </div>
      </div>
      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:16px">
        <div class="mo-card">
          <div class="mo-card-label" style="padding:14px 16px 6px">Browsers</div>
          <div style="padding:0 8px 10px">
            <mo-bar-list [rows]="data.browserRows" />
          </div>
        </div>
        <div class="mo-card">
          <div class="mo-card-label" style="padding:14px 16px 6px">Operating systems</div>
          <div style="padding:0 8px 10px">
            <mo-bar-list [rows]="data.osRows" />
          </div>
        </div>
      </div>
      <div style="margin-top:12px;font-size:12px;color:var(--color-text-secondary)">Device details come from the browser's own reported user agent — never from fingerprinting.</div>
    </section>
  `,
})
export class Devices {
  protected readonly data = inject(AnalyticsDataService);
}
