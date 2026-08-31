import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { AnalyticsDataService, fmt } from '../core/analytics-data.service';
import { areaD, lineD, linePts, sparkD } from '../core/chart';
import { Icon } from '../ui/icon';
import { InlineMessage } from '../ui/inline-message';
import { Metric } from '../ui/metric';
import { Sparkline } from '../ui/sparkline';

const GRID = 'minmax(200px,2fr) 1fr 1fr 1fr 110px';

@Component({
  selector: 'mo-events',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon, InlineMessage, Metric, Sparkline],
  template: `
    <section>
      @if (!selEvent()) {
        <div style="display:flex;align-items:baseline;gap:12px;flex-wrap:wrap;margin:4px 0 6px">
          <h1 class="mo-page-title">Events</h1>
          <span style="font-size:13px;color:var(--color-text-secondary)">Custom events you send with <span class="mo-mono" style="font-size:12px">mochi.event()</span></span>
        </div>
        <div style="margin:12px 0 16px">
          <mo-inline-message tone="info">Events are counted in aggregate and are never tied to an individual visitor.</mo-inline-message>
        </div>
        <div class="mo-card">
          <div class="mo-grid-table__head" [style.grid-template-columns]="grid">
            <span>Event</span><span style="text-align:right">Total</span><span style="text-align:right">Unique visits</span><span style="text-align:right">Conversion</span><span>Trend</span>
          </div>
          @for (e of data.events; track e.id; let i = $index) {
            <button type="button" class="mo-row-btn mo-grid-table__row" [style.grid-template-columns]="grid" (click)="open(e.id)">
              <span class="mo-mono" style="font-size:12.5px;color:var(--color-accent)">{{ e.id }}</span>
              <span class="mo-num" style="text-align:right">{{ fmt(e.total) }}</span>
              <span class="mo-num" style="text-align:right">{{ fmt(e.uniq) }}</span>
              <span class="mo-num" style="text-align:right">{{ e.conv }}</span>
              <mo-sparkline [path]="sparks[i]" />
            </button>
          }
        </div>
      } @else {
        <div style="margin:4px 0 14px">
          <button type="button" class="tr-btn tr-btn--ghost tr-btn--sm" (click)="close()">
            <mo-icon name="arrow-left" [size]="14" />
            All events
          </button>
        </div>
        <h1 class="mo-mono" style="font:600 22px/1.2 var(--font-mono);margin:0 0 18px">{{ selEvent()!.id }}</h1>
        <div class="mo-card mo-metric-strip" style="grid-template-columns:repeat(auto-fit,minmax(160px,1fr));margin-bottom:16px">
          @for (m of selEventMetrics(); track m.label) {
            <div>
              <mo-metric [label]="m.label" [value]="m.value" [delta]="m.delta" [dir]="m.dir" />
            </div>
          }
        </div>
        <div class="mo-card" style="padding:14px 16px;margin-bottom:16px">
          <div class="mo-card-label" style="margin-bottom:10px">Events · last 30 days</div>
          <svg viewBox="0 0 760 120" style="width:100%;height:auto;display:block" aria-hidden="true">
            <path [attr.d]="eventChart.area" fill="var(--color-accent-subtle)" opacity="0.65" />
            <path [attr.d]="eventChart.line" fill="none" stroke="var(--color-accent)" stroke-width="2" />
          </svg>
        </div>
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:16px">
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Pages where it fired</div>
            @for (r of selEvent()!.pages; track r[0]) {
              <div class="mo-kv-row"><span class="mo-mono" style="font-size:12px">{{ r[0] }}</span><span>{{ fmt(r[1]) }}</span></div>
            }
          </div>
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Sources</div>
            @for (r of selEvent()!.sources; track r[0]) {
              <div class="mo-kv-row"><span>{{ r[0] }}</span><span>{{ fmt(r[1]) }}</span></div>
            }
          </div>
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Devices &amp; countries</div>
            @for (r of data.selEventDevGeo; track r.name) {
              <div class="mo-kv-row"><span>{{ r.name }}</span><span>{{ r.val }}</span></div>
            }
          </div>
        </div>
      }
    </section>
  `,
})
export class Events {
  protected readonly data = inject(AnalyticsDataService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly grid = GRID;
  protected readonly fmt = fmt;

  private readonly selId = toSignal(this.route.queryParamMap.pipe(map(p => p.get('event'))), { initialValue: null });
  protected readonly selEvent = computed(() => this.data.events.find(e => e.id === this.selId()) ?? null);

  protected readonly sparks = this.data.events.map((_, i) => this.evSpark(i));

  protected readonly eventChart = (() => {
    const series = this.data.eventSeries();
    const max = Math.max(...series) * 1.1;
    const pts = linePts(series, 8, 752, 8, 112, max);
    return { line: lineD(pts), area: areaD(pts, 112) };
  })();

  protected readonly selEventMetrics = computed(() => {
    const e = this.selEvent();
    if (!e) return [];
    return [
      { label: 'Total events', value: fmt(e.total), delta: e.delta, dir: (e.delta.startsWith('−') ? 'down' : 'up') as 'up' | 'down' },
      { label: 'Unique visits', value: fmt(e.uniq), delta: '+9%', dir: 'up' as const },
      { label: 'Conversion rate', value: e.conv, delta: '+0.4 pt', dir: 'up' as const },
    ];
  });

  private evSpark(i: number): string {
    return sparkD(Array.from({ length: 14 }, (_, k) => 3 + ((k * (i + 3) * 7) % 9) + k * 0.4), 90, 24);
  }

  protected open(id: string): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { event: id } });
  }

  protected close(): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }
}
