import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { AnalyticsDataService, NameVal, PageStats, TableColumn, fmt } from '../core/analytics-data.service';
import { Icon } from '../ui/icon';
import { DataTable } from '../ui/data-table';
import { Metric } from '../ui/metric';
import { PageState } from '../ui/page-state';

const SORT_KEYS: Record<string, keyof PageStats> = {
  'Visitors': 'v',
  'Pageviews': 'pv',
  'Bounce rate': 'bounce',
  'Entry visits': 'entry',
  'Exit visits': 'exit',
};

@Component({
  selector: 'mo-pages',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon, DataTable, Metric, PageState],
  template: `
    <section>
      @if (!selPage()) {
        <div class="mo-page-head">
          <h1 class="mo-page-title">Pages</h1>
          <div class="mo-spacer"></div>
          <div class="tr-search" style="width:220px">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <circle cx="11" cy="11" r="8" /><path d="m21 21-4.3-4.3" />
            </svg>
            <input type="search" class="tr-input" placeholder="Filter pages" [value]="query()" (input)="onQuery($event)" />
          </div>
          <select class="tr-select" style="width:150px" aria-label="Sort by" (change)="onSort($event)">
            @for (o of sortOptions; track o) {
              <option [value]="o" [selected]="o === sortLabel()">{{ o }}</option>
            }
          </select>
          <button type="button" class="tr-btn tr-btn--secondary tr-btn--icon" aria-label="Reverse sort order" title="Reverse sort order" (click)="flipSort()">
            <mo-icon [name]="sortDir() === -1 ? 'arrow-down-wide-narrow' : 'arrow-up-narrow-wide'" />
          </button>
        </div>
        @if (state() !== 'ready') {
          <mo-page-state [kind]="state()" />
        } @else if (!data.pages().length) {
          <mo-page-state kind="empty" />
        } @else {
          <div class="mo-card" style="overflow-x:auto">
            <mo-data-table [columns]="pageCols" [rows]="pageRows()" [lined]="true" [clickable]="true" (rowClick)="openPage($event)" />
          </div>
          <div style="margin-top:10px;font-size:12px;color:var(--color-text-secondary)">Select a page to see its details.</div>
        }
      } @else {
        <div style="margin:4px 0 14px">
          <button type="button" class="tr-btn tr-btn--ghost tr-btn--sm" (click)="closePage()">
            <mo-icon name="arrow-left" [size]="14" />
            All pages
          </button>
        </div>
        <h1 style="font:600 24px/1.2 var(--font-display);margin:0 0 2px;letter-spacing:-.01em">{{ selPage()!.title }}</h1>
        <div class="mo-mono" style="font-size:13px;color:var(--color-text-secondary);margin-bottom:18px">{{ data.site() }}{{ selPage()!.id }}</div>
        <div class="mo-card mo-metric-strip" style="grid-template-columns:repeat(auto-fit,minmax(160px,1fr));margin-bottom:16px">
          @for (m of selPageMetrics(); track m.label) {
            <div>
              <mo-metric [label]="m.label" [value]="m.value" />
            </div>
          }
        </div>
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:16px">
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Events on this page</div>
            @for (r of selPageEvents(); track r.name) {
              <div class="mo-kv-row"><span class="mo-mono" style="font-size:12px">{{ r.name }}</span><span>{{ r.val }}</span></div>
            }
            @if (!selPageEvents().length) {
              <div style="padding:2px 10px 6px;font-size:13px;color:var(--color-text-secondary)">No custom events fired here in this period.</div>
            }
          </div>
        </div>
        <!-- Per-page referrer, device and country breakdowns need a page filter on the stats API. -->
        <div style="margin-top:12px;font-size:12px;color:var(--color-text-secondary)">Referrer, device, and country breakdowns per page are coming soon.</div>
      }
    </section>
  `,
})
export class Pages {
  protected readonly data = inject(AnalyticsDataService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly query = signal('');
  protected readonly sortKey = signal<keyof PageStats>('v');
  protected readonly sortDir = signal(-1);

  protected readonly sortOptions = Object.keys(SORT_KEYS);
  protected readonly sortLabel = computed(
    () => this.sortOptions.find(k => SORT_KEYS[k] === this.sortKey()) ?? 'Visitors',
  );

  protected readonly state = computed(() => this.data.stateOf(this.data.pagesRes));

  private readonly selPath = toSignal(this.route.queryParamMap.pipe(map(p => p.get('path'))), { initialValue: null });
  protected readonly selPage = computed(() => this.data.pages().find(p => p.id === this.selPath()) ?? null);

  protected readonly pageCols: TableColumn[] = [
    { key: 'page', label: 'Page' },
    { key: 'v', label: 'Visitors', numeric: true },
    { key: 'pv', label: 'Pageviews', numeric: true },
    { key: 'vpv', label: 'Views / visitor', numeric: true },
    { key: 'bounce', label: 'Bounce', numeric: true },
    { key: 'dur', label: 'Avg duration', numeric: true },
    { key: 'entry', label: 'Entries', numeric: true },
    { key: 'exit', label: 'Exits', numeric: true },
  ];

  protected readonly pageRows = computed(() => {
    const q = this.query().toLowerCase();
    const key = this.sortKey();
    const dir = this.sortDir();
    return this.data.pages()
      .filter(p => !q || p.id.toLowerCase().includes(q) || p.title.toLowerCase().includes(q))
      .slice()
      .sort((a, b) => ((a[key] as number) - (b[key] as number)) * dir)
      .map(p => ({
        id: p.id,
        page: p.id,
        v: fmt(p.v),
        pv: fmt(p.pv),
        vpv: p.v > 0 ? (p.pv / p.v).toFixed(1) : '0.0',
        bounce: p.bounce + '%',
        dur: p.dur,
        entry: fmt(p.entry),
        exit: fmt(p.exit),
      }));
  });

  protected readonly selPageMetrics = computed(() => {
    const p = this.selPage();
    if (!p) return [];
    return [
      { label: 'Visitors', value: fmt(p.v) },
      { label: 'Pageviews', value: fmt(p.pv) },
      { label: 'Bounce rate', value: p.bounce + '%' },
      { label: 'Avg duration', value: p.dur },
      { label: 'Entries', value: fmt(p.entry) },
      { label: 'Exits', value: fmt(p.exit) },
    ];
  });

  protected readonly selPageEvents = computed<NameVal[]>(() => {
    const path = this.selPath();
    if (!path) return [];
    return this.data.events()
      .map(e => ({ name: e.id, count: e.pages.find(p => p[0] === path)?.[1] ?? 0 }))
      .filter(e => e.count > 0)
      .sort((a, b) => b.count - a.count)
      .map(e => ({ name: e.name, val: fmt(e.count) }));
  });

  protected onQuery(e: Event): void {
    this.query.set((e.target as HTMLInputElement).value);
  }

  protected onSort(e: Event): void {
    this.sortKey.set(SORT_KEYS[(e.target as HTMLSelectElement).value] ?? 'v');
  }

  protected flipSort(): void {
    this.sortDir.update(d => -d);
  }

  protected openPage(row: Record<string, string>): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { path: row['id'] } });
  }

  protected closePage(): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }
}
