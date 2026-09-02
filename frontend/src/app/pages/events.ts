import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { AnalyticsDataService, fmt } from '../core/analytics-data.service';
import { Icon } from '../ui/icon';
import { InlineMessage } from '../ui/inline-message';
import { Metric } from '../ui/metric';
import { PageState } from '../ui/page-state';

const GRID = 'minmax(200px,2fr) 1fr 1fr 1fr';

@Component({
  selector: 'mo-events',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon, InlineMessage, Metric, PageState],
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
        @if (state() !== 'ready') {
          <mo-page-state [kind]="state()" />
        } @else if (!data.events().length) {
          <div class="mo-card" style="padding:44px 24px;text-align:center;color:var(--color-text-secondary);font-size:13.5px">
            <span style="display:block;font-weight:600;color:var(--color-text-primary);margin-bottom:4px">No events yet</span>
            <span>Send your first one with <span class="mo-mono" style="font-size:12px">mochi.event('name')</span> and it will appear here.</span>
          </div>
        } @else {
          <div class="mo-card">
            <div class="mo-grid-table__head" [style.grid-template-columns]="grid">
              <span>Event</span><span style="text-align:right">Total</span><span style="text-align:right">Unique visits</span><span style="text-align:right">Conversion</span>
            </div>
            @for (e of data.events(); track e.id) {
              <button type="button" class="mo-row-btn mo-grid-table__row" [style.grid-template-columns]="grid" (click)="open(e.id)">
                <span class="mo-mono" style="font-size:12.5px;color:var(--color-accent)">{{ e.id }}</span>
                <span class="mo-num" style="text-align:right">{{ fmt(e.total) }}</span>
                <span class="mo-num" style="text-align:right">{{ fmt(e.uniq) }}</span>
                <span class="mo-num" style="text-align:right">{{ e.conv }}</span>
              </button>
            }
          </div>
        }
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
              <mo-metric [label]="m.label" [value]="m.value" />
            </div>
          }
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

  protected readonly state = computed(() => this.data.stateOf(this.data.eventsRes));

  private readonly selId = toSignal(this.route.queryParamMap.pipe(map(p => p.get('event'))), { initialValue: null });
  protected readonly selEvent = computed(() => this.data.events().find(e => e.id === this.selId()) ?? null);

  protected readonly selEventMetrics = computed(() => {
    const e = this.selEvent();
    if (!e) return [];
    return [
      { label: 'Total events', value: fmt(e.total) },
      { label: 'Unique visits', value: fmt(e.uniq) },
      { label: 'Conversion rate', value: e.conv },
    ];
  });

  protected open(id: string): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { event: id } });
  }

  protected close(): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }
}
