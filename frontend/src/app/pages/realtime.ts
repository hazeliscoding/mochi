import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject } from '@angular/core';
import { AnalyticsDataService } from '../core/analytics-data.service';
import { BarList } from '../ui/bar-list';
import { PageState } from '../ui/page-state';

const POLL_MS = 15000;

@Component({
  selector: 'mo-realtime',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BarList, PageState],
  template: `
    <section>
      <h1 class="mo-page-title" style="margin:4px 0 20px">Realtime</h1>
      @if (state() !== 'ready') {
        <mo-page-state [kind]="state()" />
      } @else if (rt(); as r) {
        <div
          class="mo-card"
          style="padding:22px 24px;margin-bottom:16px;display:flex;align-items:center;gap:16px;flex-wrap:wrap"
        >
          <span
            style="width:10px;height:10px;border-radius:50%;background:var(--color-accent);flex:none;animation:mo-pulse 2.6s ease-out infinite"
          ></span>
          <span class="mo-num" style="font:600 40px/1 var(--font-display)">{{ r.active }}</span>
          <span style="font-size:16px"
            >active {{ r.active === 1 ? 'visit' : 'visits' }} in the last 5 minutes</span
          >
          <span class="mo-spacer"></span>
          <span style="font-size:12px;color:var(--color-text-secondary)"
            >Aggregates only, never individual profiles</span
          >
        </div>
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:16px">
          <div class="mo-card" style="padding:14px 16px">
            <div class="mo-card-label" style="margin-bottom:10px">Pageviews · last 30 minutes</div>
            <svg
              viewBox="0 0 300 72"
              style="width:100%;height:auto;display:block"
              role="img"
              aria-label="Pageviews per minute over the last 30 minutes"
            >
              @for (v of r.vals; track $index) {
                <rect
                  [attr.x]="$index * 10"
                  width="7"
                  [attr.y]="68 - barH(v, r.maxVal)"
                  [attr.height]="barH(v, r.maxVal)"
                  rx="1.5"
                  fill="var(--color-accent)"
                  [attr.opacity]="$index === 29 ? 1 : 0.45 + ($index / 29) * 0.4"
                />
              }
            </svg>
            <div
              style="display:flex;justify-content:space-between;font-size:11px;color:var(--color-text-disabled);margin-top:4px"
            >
              <span>30 min ago</span><span>now</span>
            </div>
          </div>
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Active pages</div>
            @if (r.pages.length) {
              <mo-bar-list [rows]="r.pages" [showPct]="false" [mono]="true" />
            } @else {
              <div style="padding:6px 10px;font-size:13px;color:var(--color-text-secondary)">
                No one is browsing right now.
              </div>
            }
          </div>
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Incoming sources</div>
            @if (r.sources.length) {
              <mo-bar-list [rows]="r.sources" [showPct]="false" />
            } @else {
              <div style="padding:6px 10px;font-size:13px;color:var(--color-text-secondary)">
                Quiet for the moment.
              </div>
            }
          </div>
          <div class="mo-card" style="padding:14px 16px">
            <div class="mo-card-label" style="margin-bottom:8px">Countries</div>
            @for (c of r.countries; track c.name) {
              <div style="display:flex;justify-content:space-between;padding:5px 0;font-size:13px">
                <span>{{ c.name }}</span
                ><span class="mo-muted mo-num">{{ c.val }}</span>
              </div>
            }
            @if (!r.countries.length) {
              <div style="padding:2px 0 6px;font-size:13px;color:var(--color-text-secondary)">
                No visits in the last 5 minutes.
              </div>
            }
            <div style="height:1px;background:var(--color-border-subtle);margin:10px 0"></div>
            <div class="mo-card-label" style="margin-bottom:8px">Devices</div>
            <div class="mo-split" aria-hidden="true">
              <div
                [style.width.%]="r.devicePct.desktop"
                style="background:var(--color-accent)"
              ></div>
              <div [style.width.%]="r.devicePct.mobile" style="background:var(--teal-300)"></div>
            </div>
            <div style="display:flex;gap:16px;margin-top:8px;font-size:13px">
              <span>Desktop {{ r.devices.desktop }}</span
              ><span class="mo-muted">Mobile {{ r.devices.mobile }}</span
              ><span class="mo-muted">Tablet {{ r.devices.tablet }}</span>
            </div>
          </div>
        </div>
      }
    </section>
  `,
})
export class Realtime {
  protected readonly data = inject(AnalyticsDataService);

  protected readonly rt = this.data.rt;
  protected readonly state = computed(() => this.data.stateOf(this.data.realtimeRes));

  constructor() {
    // Poll while the page is open; the interval dies with the component.
    this.data.realtimeRes.reload();
    const timer = setInterval(() => this.data.realtimeRes.reload(), POLL_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected barH(v: number, max: number): number {
    return Math.round((v / max) * 60 * 10) / 10;
  }
}
