import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { AnalyticsDataService, PageStats, TableColumn, fmt } from '../core/analytics-data.service';
import { areaD, lineD, linePts } from '../core/chart';
import { Icon } from '../ui/icon';
import { DataTable } from '../ui/data-table';
import { Metric } from '../ui/metric';

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
  imports: [Icon, DataTable, Metric],
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
        <div class="mo-card" style="overflow-x:auto">
          <mo-data-table [columns]="pageCols" [rows]="pageRows()" [lined]="true" [clickable]="true" (rowClick)="openPage($event)" />
        </div>
        <div style="margin-top:10px;font-size:12px;color:var(--color-text-secondary)">Select a page to see its trend, referrers, and events.</div>
      } @else {
        <div style="margin:4px 0 14px">
          <button type="button" class="tr-btn tr-btn--ghost tr-btn--sm" (click)="closePage()">
            <mo-icon name="arrow-left" [size]="14" />
            All pages
          </button>
        </div>
        <h1 style="font:600 24px/1.2 var(--font-display);margin:0 0 2px;letter-spacing:-.01em">{{ selPage()!.title }}</h1>
        <div class="mo-mono" style="font-size:13px;color:var(--color-text-secondary);margin-bottom:18px">hazeliscoding.com{{ selPage()!.id }}</div>
        <div class="mo-card mo-metric-strip" style="grid-template-columns:repeat(auto-fit,minmax(160px,1fr));margin-bottom:16px">
          @for (m of selPageMetrics(); track m.label) {
            <div>
              <mo-metric [label]="m.label" [value]="m.value" [delta]="m.delta" [dir]="m.dir" />
            </div>
          }
        </div>
        <div class="mo-card" style="padding:14px 16px;margin-bottom:16px">
          <div class="mo-card-label" style="margin-bottom:10px">Visitors · last 30 days</div>
          <svg viewBox="0 0 760 120" style="width:100%;height:auto;display:block" aria-hidden="true">
            <path [attr.d]="detailChart().area" fill="var(--color-accent-subtle)" opacity="0.65" />
            <path [attr.d]="detailChart().line" fill="none" stroke="var(--color-accent)" stroke-width="2" />
          </svg>
        </div>
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:16px">
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Top referrers</div>
            @for (r of data.selPageRefs; track r.name) {
              <div class="mo-kv-row"><span>{{ r.name }}</span><span>{{ r.val }}</span></div>
            }
          </div>
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Devices</div>
            @for (r of data.selPageDevices; track r.name) {
              <div class="mo-kv-row"><span>{{ r.name }}</span><span>{{ r.val }}</span></div>
            }
          </div>
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Countries</div>
            @for (r of data.selPageCountries; track r.name) {
              <div class="mo-kv-row"><span>{{ r.name }}</span><span>{{ r.val }}</span></div>
            }
          </div>
          <div class="mo-card" style="padding:14px 8px">
            <div class="mo-card-label" style="margin:0 8px 8px">Events on this page</div>
            @for (r of data.selPageEvents; track r.name) {
              <div class="mo-kv-row"><span class="mo-mono" style="font-size:12px">{{ r.name }}</span><span>{{ r.val }}</span></div>
            }
          </div>
        </div>
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

  private readonly selPath = toSignal(this.route.queryParamMap.pipe(map(p => p.get('path'))), { initialValue: null });
  protected readonly selPage = computed(() => this.data.pages.find(p => p.id === this.selPath()) ?? null);

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
    return this.data.pages
      .filter(p => !q || p.id.toLowerCase().includes(q) || p.title.toLowerCase().includes(q))
      .slice()
      .sort((a, b) => ((a[key] as number) - (b[key] as number)) * dir)
      .map(p => ({
        id: p.id,
        page: p.id,
        v: fmt(p.v),
        pv: fmt(p.pv),
        vpv: (p.pv / p.v).toFixed(1),
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
      { label: 'Visitors', value: fmt(p.v), delta: '+9%', dir: 'up' as const },
      { label: 'Pageviews', value: fmt(p.pv), delta: '+7%', dir: 'up' as const },
      { label: 'Bounce rate', value: p.bounce + '%', delta: '−1.2 pt', dir: 'down' as const },
      { label: 'Avg duration', value: p.dur, delta: '+4s', dir: 'up' as const },
    ];
  });

  protected readonly detailChart = computed(() => {
    const p = this.selPage();
    if (!p) return { line: '', area: '' };
    const series = this.data.pageDetailSeries(p);
    const max = Math.max(...series) * 1.1;
    const pts = linePts(series, 8, 752, 8, 112, max);
    return { line: lineD(pts), area: areaD(pts, 112) };
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
