import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AnalyticsDataService, fmt } from '../core/analytics-data.service';
import { sparkD } from '../core/chart';
import { Dialog } from '../ui/dialog';
import { Icon } from '../ui/icon';
import { Sparkline } from '../ui/sparkline';
import { Tag } from '../ui/tag';

const GRID = 'minmax(200px,2fr) 110px 1fr 1fr 110px';

@Component({
  selector: 'mo-goals',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, Icon, Sparkline, Tag],
  template: `
    <section>
      <div class="mo-page-head">
        <h1 class="mo-page-title">Goals</h1>
        <div class="mo-spacer"></div>
        <button type="button" class="tr-btn tr-btn--primary" (click)="dialogOpen.set(true)">
          <mo-icon name="plus" [size]="14" />
          Create goal
        </button>
      </div>
      <div class="mo-card">
        <div class="mo-grid-table__head" [style.grid-template-columns]="grid">
          <span>Goal</span><span>Type</span><span style="text-align:right">Conversions</span><span style="text-align:right">Conversion rate</span><span>Trend</span>
        </div>
        @for (g of data.goals; track g.name; let i = $index) {
          <div class="mo-grid-table__row" [style.grid-template-columns]="grid" style="padding:11px 16px;border-bottom:1px solid var(--color-border-subtle)">
            <span style="font-weight:600">{{ g.name }}</span>
            <mo-tag>{{ g.type }}</mo-tag>
            <span class="mo-num" style="text-align:right">{{ fmt(g.conv) }}</span>
            <span class="mo-num" style="text-align:right">{{ g.rate }}</span>
            <mo-sparkline [path]="sparks[i]" />
          </div>
        }
      </div>
      <div style="margin-top:10px;font-size:12px;color:var(--color-text-secondary)">Conversion rate is unique conversions divided by unique visits in the period.</div>

      <mo-dialog [open]="dialogOpen()" title="Create goal" (closed)="dialogOpen.set(false)">
        <div style="display:flex;flex-direction:column;gap:14px;min-width:340px">
          <div>
            <div class="mo-card-label" style="margin-bottom:8px">What counts as a conversion?</div>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px">
              @for (t of data.goalTypes; track t[0]) {
                <label
                  style="display:flex;gap:8px;align-items:flex-start;border-radius:3px;padding:10px 12px;cursor:pointer"
                  [style.border]="'1px solid ' + (goalType() === t[0] ? 'var(--color-accent)' : 'var(--color-border)')"
                  [style.background]="goalType() === t[0] ? 'var(--color-accent-subtle)' : 'var(--color-surface)'"
                >
                  <input type="radio" name="goaltype" [checked]="goalType() === t[0]" (change)="goalType.set(t[0])" style="margin-top:2px;accent-color:var(--color-accent)" />
                  <span><span style="font-weight:600;display:block;color:var(--color-text-primary)">{{ t[1] }}</span><span style="font-size:12px;color:var(--color-text-secondary)">{{ t[2] }}</span></span>
                </label>
              }
            </div>
          </div>
          <div>
            <label class="tr-label" for="goal-name">Goal name</label>
            <input id="goal-name" type="text" class="tr-input" placeholder="e.g. Downloaded résumé" />
          </div>
          <div>
            <label class="tr-label" for="goal-target">{{ target()[0] }}</label>
            <input id="goal-target" type="text" class="tr-input" [placeholder]="target()[1]" />
          </div>
          <div style="display:flex;justify-content:flex-end;gap:8px;margin-top:4px">
            <button type="button" class="tr-btn tr-btn--secondary" (click)="dialogOpen.set(false)">Cancel</button>
            <button type="button" class="tr-btn tr-btn--primary" (click)="dialogOpen.set(false)">Create goal</button>
          </div>
        </div>
      </mo-dialog>
    </section>
  `,
})
// Still on mock data; wiring lands with the goals stats endpoint (backend ships goal CRUD only).
export class Goals {
  protected readonly data = inject(AnalyticsDataService);

  protected readonly grid = GRID;
  protected readonly fmt = fmt;
  protected readonly dialogOpen = signal(false);
  protected readonly goalType = signal('event');

  protected readonly target = computed(() => this.data.goalTargetByType[this.goalType()]);

  protected readonly sparks = this.data.goals.map((_, i) =>
    sparkD(Array.from({ length: 14 }, (_, k) => 3 + ((k * (i + 5) * 7) % 9) + k * 0.4), 90, 24),
  );
}
