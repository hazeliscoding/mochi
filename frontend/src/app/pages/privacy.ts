import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { AnalyticsDataService, fmt } from '../core/analytics-data.service';
import { InlineMessage } from '../ui/inline-message';
import { PageState } from '../ui/page-state';

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

@Component({
  selector: 'mo-privacy',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [InlineMessage, PageState],
  template: `
    <section style="max-width:760px">
      <h1 class="mo-page-title" style="margin:4px 0 6px">Privacy center</h1>
      <div
        style="font-size:14px;color:var(--color-text-secondary);margin-bottom:22px;max-width:560px"
      >
        Mochi measures your website, not your visitors. Here is exactly what that means, no legal
        document required.
      </div>

      @if (state() !== 'ready') {
        <mo-page-state [kind]="state()" />
      } @else {
        <div
          style="display:grid;grid-template-columns:repeat(auto-fit,minmax(230px,1fr));gap:10px;margin-bottom:26px"
        >
          @for (c of data.privChecks; track c[0]) {
            <div
              class="mo-card"
              style="display:flex;gap:10px;align-items:flex-start;padding:12px 14px"
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="var(--color-success)"
                stroke-width="2.5"
                stroke-linecap="round"
                stroke-linejoin="round"
                style="flex:none;margin-top:2px"
                aria-hidden="true"
              >
                <path d="M20 6 9 17l-5-5" />
              </svg>
              <span
                ><span style="font-weight:600;display:block">{{ c[0] }}</span
                ><span style="font-size:12.5px;color:var(--color-text-secondary)">{{
                  c[1]
                }}</span></span
              >
            </div>
          }
        </div>

        <div
          style="display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:16px;margin-bottom:26px"
        >
          <div class="mo-card" style="padding:16px 18px">
            <div class="mo-card-label" style="margin-bottom:10px">Data we collect</div>
            @for (i of data.collectedItems; track i[0]) {
              <div
                style="padding:5px 0;font-size:13.5px;border-bottom:1px solid var(--color-border-subtle)"
              >
                {{ i[0] }}
                <span style="color:var(--color-text-secondary);font-size:12.5px">· {{ i[1] }}</span>
              </div>
            }
          </div>
          <div class="mo-card" style="padding:16px 18px">
            <div class="mo-card-label" style="margin-bottom:10px">Data we refuse to collect</div>
            @for (i of data.notCollectedItems; track i[0]) {
              <div
                style="padding:5px 0;font-size:13.5px;border-bottom:1px solid var(--color-border-subtle);display:flex;gap:8px"
              >
                <span style="color:var(--color-text-disabled)" aria-hidden="true">✕</span
                ><span
                  >{{ i[0] }}
                  <span style="color:var(--color-text-secondary);font-size:12.5px"
                    >· {{ i[1] }}</span
                  ></span
                >
              </div>
            }
          </div>
        </div>

        <div class="mo-card" style="padding:16px 18px;margin-bottom:16px">
          <div class="mo-card-label" style="margin-bottom:4px">What Mochi holds right now</div>
          <div style="font-size:13px;color:var(--color-text-secondary);margin-bottom:14px">
            Queried live for {{ data.site() }}. These are facts, not promises.
          </div>
          <div
            style="display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:14px"
          >
            <div>
              <div style="font-size:22px;font-weight:700;font-variant-numeric:tabular-nums">
                {{ rawHeld() }}
              </div>
              <div style="font-size:12.5px;color:var(--color-text-secondary)">
                raw events held, each deleted {{ priv().rawEventLifetimeDays }} days after it
                arrives
              </div>
            </div>
            <div>
              <div style="font-size:22px;font-weight:700">{{ oldestLabel() }}</div>
              <div style="font-size:12.5px;color:var(--color-text-secondary)">
                oldest daily aggregate on record
              </div>
            </div>
            <div>
              <div style="font-size:22px;font-weight:700">
                {{ data.retentionLabel(priv().retention) }}
              </div>
              <div style="font-size:12.5px;color:var(--color-text-secondary)">
                current retention setting
              </div>
            </div>
          </div>
        </div>

        <div class="mo-card" style="padding:16px 18px;margin-bottom:16px">
          <div class="mo-card-label" style="margin-bottom:4px">Data retention</div>
          <div style="font-size:13px;color:var(--color-text-secondary);margin-bottom:12px">
            How long Mochi keeps daily aggregates for this website. Saved the moment you pick one;
            the Settings page shows the same value.
          </div>
          <div style="display:flex;flex-direction:column;gap:8px">
            @for (r of data.retentionChoices; track r[0]) {
              <label style="display:flex;gap:10px;align-items:center;cursor:pointer">
                <input
                  type="radio"
                  name="retention"
                  [checked]="retention() === r[0]"
                  (change)="setRetention(r[0])"
                  [disabled]="saving()"
                  style="accent-color:var(--color-accent)"
                />
                <span style="font-weight:600;min-width:190px">{{ r[1] }}</span>
                <span style="font-size:12.5px;color:var(--color-text-secondary)">{{ r[2] }}</span>
              </label>
            }
          </div>
          <div
            style="min-height:22px;margin-top:8px;display:flex;align-items:center;gap:10px"
            aria-live="polite"
          >
            @if (saving()) {
              <span style="font-size:13px;color:var(--color-text-secondary)">Saving…</span>
            } @else if (saved()) {
              <span style="font-size:13px;color:var(--color-success)">Saved.</span>
            } @else if (saveError()) {
              <mo-inline-message tone="danger">{{ saveError() }}</mo-inline-message>
            }
          </div>
        </div>

        <div
          class="mo-card"
          style="padding:16px 18px;margin-bottom:16px;display:flex;align-items:center;justify-content:space-between;gap:16px;flex-wrap:wrap"
        >
          <span style="max-width:480px">
            <span class="mo-card-label" style="display:block;margin-bottom:4px"
              >Your data, portable</span
            >
            <span style="font-size:13px;color:var(--color-text-secondary)"
              >One zip with a CSV per daily aggregate table plus your goals. Everything Mochi has
              for this site, ready to leave with you.</span
            >
          </span>
          <a class="tr-btn tr-btn--secondary" [href]="exportHref()" download>Export all data</a>
        </div>

        <div class="mo-card" style="padding:16px 18px">
          <div class="mo-card-label" style="margin-bottom:8px">How visitors stay anonymous</div>
          <p style="margin:0;font-size:13.5px;line-height:1.55;max-width:560px">
            Each visit is counted under a hash scoped to a single day: a random salt is mixed in at
            ingestion and destroyed at the UTC day boundary, never stored. Once the salt is gone the
            hash cannot be recomputed, so the same visitor on two different days can never be
            linked. No per-visitor record exists to export, hand over, or leak.
          </p>
        </div>
      }
    </section>
  `,
})
export class Privacy {
  protected readonly data = inject(AnalyticsDataService);

  // 'empty' when the account has no sites; otherwise follow the two resources.
  protected readonly state = computed(() => {
    if (this.data.stateOf(this.data.sitesRes) === 'ready' && !this.data.currentSite())
      return 'empty' as const;
    return this.data.stateOf(this.data.sitesRes, this.data.privacyRes);
  });

  protected readonly priv = computed(() => this.data.privacyRes.value()!);
  protected readonly rawHeld = computed(() => fmt(this.priv().rawEventsHeld));

  protected readonly oldestLabel = computed(() => {
    const iso = this.priv().oldestAggregateDate;
    if (!iso) return 'No aggregates yet';
    const [y, m, d] = iso.split('-').map(Number);
    return `${MONTHS[m - 1]} ${d}, ${y}`;
  });

  protected exportHref(): string {
    return this.data.exportUrl(this.data.siteId() ?? '');
  }

  // Optimistic radio state; the reload after a save snaps it back to the server value.
  protected readonly retention = linkedSignal(
    () => this.data.privacyRes.value()?.retention ?? '1y',
  );

  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly saveError = signal('');
  private savedTimer: ReturnType<typeof setTimeout> | undefined;

  protected setRetention(wire: string): void {
    const id = this.data.siteId();
    if (!id || wire === this.retention()) return;
    this.retention.set(wire);
    this.saving.set(true);
    this.saved.set(false);
    this.saveError.set('');
    this.data.updateSite(id, { retention: wire }).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
        clearTimeout(this.savedTimer);
        this.savedTimer = setTimeout(() => this.saved.set(false), 2500);
      },
      error: () => {
        this.saving.set(false);
        this.saveError.set('Could not save the retention setting. Try again.');
      },
    });
  }
}
