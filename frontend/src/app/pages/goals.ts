import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AnalyticsDataService, GoalStats, fmt } from '../core/analytics-data.service';
import { Dialog } from '../ui/dialog';
import { Icon } from '../ui/icon';
import { InlineMessage } from '../ui/inline-message';
import { PageState } from '../ui/page-state';
import { Tag } from '../ui/tag';

const GRID = 'minmax(200px,2fr) 110px 1fr 1fr 40px';

@Component({
  selector: 'mo-goals',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, Icon, InlineMessage, PageState, Tag],
  template: `
    <section>
      <div class="mo-page-head">
        <h1 class="mo-page-title">Goals</h1>
        <div class="mo-spacer"></div>
        <button type="button" class="tr-btn tr-btn--primary" (click)="openCreate()">
          <mo-icon name="plus" [size]="14" />
          Create goal
        </button>
      </div>
      @if (state() !== 'ready') {
        <mo-page-state [kind]="state()" />
      } @else if (!data.goals().length) {
        <div
          class="mo-card"
          style="padding:44px 24px;text-align:center;color:var(--color-text-secondary);font-size:13.5px"
        >
          <span
            style="display:block;font-weight:600;color:var(--color-text-primary);margin-bottom:4px"
            >No goals yet</span
          >
          <span>Create your first goal to start counting conversions.</span>
        </div>
      } @else {
        <div class="mo-card">
          <div class="mo-grid-table__head" [style.grid-template-columns]="grid">
            <span>Goal</span><span>Type</span><span style="text-align:right">Conversions</span
            ><span style="text-align:right">Conversion rate</span><span></span>
          </div>
          @for (g of data.goals(); track g.id) {
            <div
              class="mo-grid-table__row"
              [style.grid-template-columns]="grid"
              style="padding:11px 16px;border-bottom:1px solid var(--color-border-subtle)"
            >
              <span>
                <span style="font-weight:600;display:block">{{ g.name }}</span>
                <span class="mo-mono" style="font-size:11.5px;color:var(--color-text-secondary)">{{
                  g.target
                }}</span>
              </span>
              <mo-tag>{{ g.type }}</mo-tag>
              <span class="mo-num" style="text-align:right">{{ fmt(g.conv) }}</span>
              <span class="mo-num" style="text-align:right">{{ g.rate }}</span>
              <button
                type="button"
                class="tr-btn tr-btn--ghost tr-btn--icon"
                [attr.aria-label]="'Delete ' + g.name"
                [title]="'Delete ' + g.name"
                (click)="openDelete(g)"
              >
                <mo-icon name="trash-2" [size]="14" />
              </button>
            </div>
          }
        </div>
        <div style="margin-top:10px;font-size:12px;color:var(--color-text-secondary)">
          Conversion rate is unique conversions divided by unique visits in the period.
        </div>
      }

      <mo-dialog [open]="dialogOpen()" title="Create goal" (closed)="dialogOpen.set(false)">
        <div style="display:flex;flex-direction:column;gap:14px;min-width:340px">
          <div>
            <div class="mo-card-label" style="margin-bottom:8px">What counts as a conversion?</div>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px">
              @for (t of data.goalTypes; track t[0]) {
                <label
                  style="display:flex;gap:8px;align-items:flex-start;border-radius:3px;padding:10px 12px;cursor:pointer"
                  [style.border]="
                    '1px solid ' +
                    (goalType() === t[0] ? 'var(--color-accent)' : 'var(--color-border)')
                  "
                  [style.background]="
                    goalType() === t[0] ? 'var(--color-accent-subtle)' : 'var(--color-surface)'
                  "
                >
                  <input
                    type="radio"
                    name="goaltype"
                    [checked]="goalType() === t[0]"
                    (change)="goalType.set(t[0])"
                    style="margin-top:2px;accent-color:var(--color-accent)"
                  />
                  <span
                    ><span style="font-weight:600;display:block;color:var(--color-text-primary)">{{
                      t[1]
                    }}</span
                    ><span style="font-size:12px;color:var(--color-text-secondary)">{{
                      t[2]
                    }}</span></span
                  >
                </label>
              }
            </div>
          </div>
          <div>
            <label class="tr-label" for="goal-name">Goal name</label>
            <input
              id="goal-name"
              type="text"
              class="tr-input"
              placeholder="e.g. Downloaded résumé"
              [value]="name()"
              (input)="name.set(val($event))"
            />
          </div>
          <div>
            <label class="tr-label" for="goal-target">{{ target()[0] }}</label>
            <input
              id="goal-target"
              type="text"
              class="tr-input"
              [placeholder]="target()[1]"
              [value]="targetVal()"
              (input)="targetVal.set(val($event))"
            />
          </div>
          @if (createError()) {
            <mo-inline-message tone="danger">{{ createError() }}</mo-inline-message>
          }
          <div style="display:flex;justify-content:flex-end;gap:8px;margin-top:4px">
            <button type="button" class="tr-btn tr-btn--secondary" (click)="dialogOpen.set(false)">
              Cancel
            </button>
            <button
              type="button"
              class="tr-btn tr-btn--primary"
              [disabled]="creating()"
              (click)="create()"
            >
              {{ creating() ? 'Creating…' : 'Create goal' }}
            </button>
          </div>
        </div>
      </mo-dialog>

      <mo-dialog [open]="!!delGoal()" title="Delete goal?" (closed)="delGoal.set(null)">
        <div style="max-width:380px;display:flex;flex-direction:column;gap:14px">
          <p style="margin:0;font-size:13.5px;line-height:1.55">
            This deletes <strong>{{ delGoal()?.name }}</strong
            >. Past conversions stay in your aggregates; only the goal definition is removed.
          </p>
          @if (deleteError()) {
            <mo-inline-message tone="danger">{{ deleteError() }}</mo-inline-message>
          }
          <div style="display:flex;justify-content:flex-end;gap:8px">
            <button type="button" class="tr-btn tr-btn--secondary" (click)="delGoal.set(null)">
              Cancel
            </button>
            <button
              type="button"
              class="tr-btn tr-btn--danger"
              [disabled]="deleting()"
              (click)="doDelete()"
            >
              {{ deleting() ? 'Deleting…' : 'Delete goal' }}
            </button>
          </div>
        </div>
      </mo-dialog>
    </section>
  `,
})
export class Goals {
  protected readonly data = inject(AnalyticsDataService);

  protected readonly grid = GRID;
  protected readonly fmt = fmt;

  protected readonly state = computed(() =>
    this.data.stateOf(this.data.goalsRes, this.data.goalStatsRes),
  );

  protected readonly dialogOpen = signal(false);
  protected readonly goalType = signal('event');
  protected readonly name = signal('');
  protected readonly targetVal = signal('');
  protected readonly creating = signal(false);
  protected readonly createError = signal('');

  protected readonly delGoal = signal<GoalStats | null>(null);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal('');

  protected readonly target = computed(() => this.data.goalTargetByType[this.goalType()]);

  protected val(e: Event): string {
    return (e.target as HTMLInputElement).value;
  }

  protected openCreate(): void {
    this.name.set('');
    this.targetVal.set('');
    this.createError.set('');
    this.dialogOpen.set(true);
  }

  protected create(): void {
    const siteId = this.data.siteId();
    if (!siteId) return;
    const name = this.name().trim();
    const target = this.targetVal().trim();
    if (!name || !target) {
      this.createError.set('Name and target are both required.');
      return;
    }
    this.creating.set(true);
    this.createError.set('');
    this.data.createGoal(siteId, { name, type: this.goalType(), target }).subscribe({
      next: () => {
        this.creating.set(false);
        this.dialogOpen.set(false);
        this.reload();
      },
      error: () => {
        this.creating.set(false);
        this.createError.set('Could not create the goal. Try again.');
      },
    });
  }

  protected openDelete(g: GoalStats): void {
    this.deleteError.set('');
    this.delGoal.set(g);
  }

  protected doDelete(): void {
    const siteId = this.data.siteId();
    const goal = this.delGoal();
    if (!siteId || !goal) return;
    this.deleting.set(true);
    this.data.deleteGoal(siteId, goal.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.delGoal.set(null);
        this.reload();
      },
      error: () => {
        this.deleting.set(false);
        this.deleteError.set('Could not delete the goal. Try again.');
      },
    });
  }

  private reload(): void {
    this.data.goalsRes.reload();
    this.data.goalStatsRes.reload();
  }
}
