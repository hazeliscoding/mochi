import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AnalyticsDataService } from '../core/analytics-data.service';
import { CodeBlock } from '../ui/code-block';
import { Dialog } from '../ui/dialog';
import { InlineMessage } from '../ui/inline-message';
import { PageState } from '../ui/page-state';
import { StatusIndicator } from '../ui/status-indicator';

@Component({
  selector: 'mo-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CodeBlock, Dialog, InlineMessage, PageState, StatusIndicator],
  template: `
    <section style="max-width:680px">
      <h1 class="mo-page-title" style="margin:4px 0 22px">Website settings</h1>
      @if (state() !== 'ready') {
        <mo-page-state [kind]="state()" />
      } @else if (!data.currentSite()) {
        <mo-page-state kind="empty" />
      } @else {
        <div style="display:flex;flex-direction:column;gap:26px">
          <div>
            <div class="mo-section-title">General</div>
            <div style="display:flex;flex-direction:column;gap:14px">
              <div>
                <label class="tr-label" for="set-name">Site name</label>
                <input id="set-name" type="text" class="tr-input" [value]="name()" (input)="name.set(val($event))" />
              </div>
              <div>
                <label class="tr-label" for="set-domain">Domain</label>
                <input id="set-domain" type="text" class="tr-input" [value]="data.currentSite()!.domain" disabled />
                <div class="tr-hint">The domain is fixed once the site is created.</div>
              </div>
              <div>
                <label class="tr-label" for="set-tz">Time zone</label>
                <select id="set-tz" class="tr-select" (change)="tz.set(val($event))">
                  @for (t of tzChoices(); track t[1]) {
                    <option [value]="t[1]" [selected]="t[1] === tz()">{{ t[0] }}</option>
                  }
                </select>
              </div>
            </div>
          </div>

          <div>
            <div class="mo-section-title">Analytics</div>
            <div style="display:flex;flex-direction:column;gap:14px">
              <div>
                <label class="tr-label" for="set-excluded">Excluded paths</label>
                <textarea id="set-excluded" class="tr-input" rows="2">/admin/*&#10;/drafts/*</textarea>
                <div class="tr-hint">One pattern per line. These pages are never counted.</div>
              </div>
              <label class="tr-choice" style="align-items:center">
                <input type="checkbox" role="switch" class="tr-switch" [checked]="qsIgnore()" (change)="qsIgnore.set(chk($event))" />
                <span>Ignore query parameters</span>
              </label>
              <label class="tr-choice" style="align-items:center">
                <input type="checkbox" role="switch" class="tr-switch" [checked]="botFilter()" (change)="botFilter.set(chk($event))" />
                <span>Filter known bots and crawlers</span>
              </label>
              <label class="tr-choice" style="align-items:center">
                <input type="checkbox" role="switch" class="tr-switch" [checked]="selfFilter()" (change)="selfFilter.set(chk($event))" />
                <span>Exclude my own visits</span>
              </label>
            </div>
          </div>

          <div>
            <div class="mo-section-title">Privacy</div>
            <div style="display:flex;flex-direction:column;gap:14px">
              <div>
                <label class="tr-label" for="set-retention">Data retention</label>
                <select id="set-retention" class="tr-select" (change)="retention.set(val($event))">
                  @for (r of data.retentionChoices; track r[0]) {
                    <option [value]="r[0]" [selected]="r[0] === retention()">{{ r[1] }}</option>
                  }
                </select>
              </div>
              <div>
                <label class="tr-label" for="set-geo">Geographic precision</label>
                <select id="set-geo" class="tr-select" (change)="geoPrecision.set(val($event))">
                  @for (g of geoOptions; track g) {
                    <option [value]="g" [selected]="g === geoPrecision()">{{ g }}</option>
                  }
                </select>
                <div class="tr-hint">Country is the default. Region requires enough traffic to stay anonymous.</div>
              </div>
              <div style="display:flex;align-items:center;gap:10px;font-size:13.5px">
                <mo-status tone="success">IP anonymization</mo-status>
                <span style="color:var(--color-text-secondary)">Always on. Addresses are discarded at ingestion and never stored.</span>
              </div>
            </div>
          </div>

          <div style="display:flex;align-items:center;gap:10px">
            <button type="button" class="tr-btn tr-btn--primary" [disabled]="saving()" (click)="save()">{{ saving() ? 'Saving…' : 'Save changes' }}</button>
            @if (saved()) {
              <span style="font-size:13px;color:var(--color-success)">Saved.</span>
            }
            @if (saveError()) {
              <mo-inline-message tone="danger">{{ saveError() }}</mo-inline-message>
            }
          </div>

          <div>
            <div class="mo-section-title">Tracking script</div>
            <div style="display:flex;align-items:center;gap:10px;font-size:13.5px;margin-bottom:12px">
              @if (siteActive()) {
                <mo-status tone="success">Installed</mo-status>
                <span style="color:var(--color-text-secondary)">Events received in the last 30 days.</span>
              } @else {
                <mo-status tone="warning">Waiting for data</mo-status>
                <span style="color:var(--color-text-secondary)">No events received yet.</span>
              }
            </div>
            <mo-code-block filename="index.html" [code]="data.snippet()" />
          </div>

          <div>
            <div class="mo-section-title">Data</div>
            <div style="border:1px solid var(--color-danger);border-radius:6px;padding:14px 16px">
              <div style="display:flex;align-items:center;justify-content:space-between;gap:16px;flex-wrap:wrap">
                <span style="font-size:13.5px"><span style="font-weight:600;display:block">Delete website</span><span style="color:var(--color-text-secondary);font-size:12.5px">Removes the site and all of its analytics from Mochi. This cannot be undone.</span></span>
                <button type="button" class="tr-btn tr-btn--danger" (click)="openDelete()">Delete website</button>
              </div>
            </div>
          </div>
        </div>

        <mo-dialog [open]="delOpen()" title="Delete website?" (closed)="delOpen.set(false)">
          <div style="max-width:380px;display:flex;flex-direction:column;gap:14px">
            <p style="margin:0;font-size:13.5px;line-height:1.55">This permanently deletes <strong>{{ data.currentSite()!.domain }}</strong> and every aggregate Mochi has stored for it. There is no undo and no recovery window.</p>
            <div>
              <label class="tr-label" for="del-confirm">Type the domain to confirm</label>
              <input id="del-confirm" type="text" class="tr-input" [placeholder]="data.currentSite()!.domain" [value]="delConfirm()" (input)="delConfirm.set(val($event))" />
            </div>
            @if (deleteError()) {
              <mo-inline-message tone="danger">{{ deleteError() }}</mo-inline-message>
            }
            <div style="display:flex;justify-content:flex-end;gap:8px">
              <button type="button" class="tr-btn tr-btn--secondary" (click)="delOpen.set(false)">Cancel</button>
              <button type="button" class="tr-btn tr-btn--danger" [disabled]="delConfirm() !== data.currentSite()!.domain || deleting()" (click)="doDelete()">{{ deleting() ? 'Deleting…' : 'Delete website' }}</button>
            </div>
          </div>
        </mo-dialog>
      }
    </section>
  `,
})
export class Settings {
  protected readonly data = inject(AnalyticsDataService);
  private readonly router = inject(Router);

  protected readonly state = computed(() => this.data.stateOf(this.data.sitesRes));

  // Form fields reset whenever the selected site changes.
  protected readonly name = linkedSignal(() => this.data.currentSite()?.name ?? '');
  protected readonly tz = linkedSignal(() => this.data.currentSite()?.timezone ?? 'America/New_York');
  protected readonly retention = linkedSignal(() => this.data.currentSite()?.retention ?? '1y');

  /** Known timezones plus the site's own when it is not in the preset list. */
  protected readonly tzChoices = computed<[string, string][]>(() => {
    const current = this.data.currentSite()?.timezone;
    const known = this.data.tzOptions;
    if (current && !known.some(t => t[1] === current)) return [[current, current], ...known];
    return known;
  });

  protected readonly siteActive = computed(
    () => this.data.sites().find(s => s.id === this.data.siteId())?.status === 'Active',
  );

  // Local-only toggles; not backed by the API yet.
  protected readonly qsIgnore = signal(true);
  protected readonly botFilter = signal(true);
  protected readonly selfFilter = signal(false);
  protected readonly geoPrecision = signal('Country only');
  protected readonly geoOptions = ['Country only', 'Country + region (thresholded)'];

  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly saveError = signal('');

  protected readonly delOpen = signal(false);
  protected readonly delConfirm = signal('');
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal('');

  protected val(e: Event): string {
    return (e.target as HTMLInputElement | HTMLSelectElement).value;
  }

  protected chk(e: Event): boolean {
    return (e.target as HTMLInputElement).checked;
  }

  protected save(): void {
    const id = this.data.siteId();
    if (!id) return;
    this.saving.set(true);
    this.saved.set(false);
    this.saveError.set('');
    this.data
      .updateSite(id, { name: this.name().trim(), timezone: this.tz(), retention: this.retention() })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.saved.set(true);
        },
        error: () => {
          this.saving.set(false);
          this.saveError.set('Could not save the settings. Try again.');
        },
      });
  }

  protected openDelete(): void {
    this.delConfirm.set('');
    this.deleteError.set('');
    this.delOpen.set(true);
  }

  protected doDelete(): void {
    const id = this.data.siteId();
    if (!id) return;
    this.deleting.set(true);
    this.data.deleteSite(id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.delOpen.set(false);
        this.data.siteId.set(null);
        this.data.sitesRes.reload();
        this.router.navigate(['/websites']);
      },
      error: () => {
        this.deleting.set(false);
        this.deleteError.set('Could not delete the website. Try again.');
      },
    });
  }
}
