import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AnalyticsDataService } from '../core/analytics-data.service';
import { CodeBlock } from '../ui/code-block';
import { Dialog } from '../ui/dialog';
import { Icon } from '../ui/icon';
import { StatusIndicator } from '../ui/status-indicator';

@Component({
  selector: 'mo-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CodeBlock, Dialog, Icon, StatusIndicator],
  template: `
    <section style="max-width:680px">
      <h1 class="mo-page-title" style="margin:4px 0 22px">Website settings</h1>
      <div style="display:flex;flex-direction:column;gap:26px">
        <div>
          <div class="mo-section-title">General</div>
          <div style="display:flex;flex-direction:column;gap:14px">
            <div>
              <label class="tr-label" for="set-name">Site name</label>
              <input id="set-name" type="text" class="tr-input" value="hazeliscoding" />
            </div>
            <div>
              <label class="tr-label" for="set-domain">Domain</label>
              <input id="set-domain" type="text" class="tr-input" value="hazeliscoding.com" />
            </div>
            <div>
              <label class="tr-label" for="set-tz">Time zone</label>
              <select id="set-tz" class="tr-select" (change)="tz.set(sel($event))">
                @for (t of data.tzOptions; track t) {
                  <option [value]="t" [selected]="t === tz()">{{ t }}</option>
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
              <select id="set-retention" class="tr-select" (change)="data.retention.set(sel($event))">
                @for (r of data.retentionOptions; track r[0]) {
                  <option [value]="r[0]" [selected]="r[0] === data.retention()">{{ r[0] }}</option>
                }
              </select>
            </div>
            <div>
              <label class="tr-label" for="set-geo">Geographic precision</label>
              <select id="set-geo" class="tr-select" (change)="geoPrecision.set(sel($event))">
                @for (g of geoOptions; track g) {
                  <option [value]="g" [selected]="g === geoPrecision()">{{ g }}</option>
                }
              </select>
              <div class="tr-hint">Country is the default. Region requires enough traffic to stay anonymous.</div>
            </div>
            <div style="display:flex;align-items:center;gap:10px;font-size:13.5px">
              <mo-status tone="success">IP anonymization</mo-status>
              <span style="color:var(--color-text-secondary)">Always on — addresses are discarded at ingestion and never stored.</span>
            </div>
          </div>
        </div>

        <div>
          <div class="mo-section-title">Tracking script</div>
          <div style="display:flex;align-items:center;gap:10px;font-size:13.5px;margin-bottom:12px">
            <mo-status tone="success">Installed</mo-status>
            <span style="color:var(--color-text-secondary)">Last event received 2 minutes ago.</span>
          </div>
          <mo-code-block filename="index.html" [code]="data.siteTag" />
        </div>

        <div>
          <div class="mo-section-title">Data</div>
          <div style="display:flex;flex-direction:column;gap:12px">
            <div style="display:flex;align-items:center;justify-content:space-between;gap:16px;flex-wrap:wrap">
              <span style="font-size:13.5px">Export all analytics for this website as CSV.</span>
              <button type="button" class="tr-btn tr-btn--secondary">
                <mo-icon name="download" [size]="14" />
                Export analytics
              </button>
            </div>
            <div style="border:1px solid var(--color-danger);border-radius:6px;padding:14px 16px;display:flex;flex-direction:column;gap:12px">
              <div style="display:flex;align-items:center;justify-content:space-between;gap:16px;flex-wrap:wrap">
                <span style="font-size:13.5px"><span style="font-weight:600;display:block">Delete analytics data</span><span style="color:var(--color-text-secondary);font-size:12.5px">Removes every aggregate for this site. The site itself stays connected.</span></span>
                <button type="button" class="tr-btn tr-btn--danger" (click)="delOpen.set(true)">Delete data</button>
              </div>
              <div style="height:1px;background:var(--color-border-subtle)"></div>
              <div style="display:flex;align-items:center;justify-content:space-between;gap:16px;flex-wrap:wrap">
                <span style="font-size:13.5px"><span style="font-weight:600;display:block">Delete website</span><span style="color:var(--color-text-secondary);font-size:12.5px">Removes the site and all of its analytics from Mochi. This cannot be undone.</span></span>
                <button type="button" class="tr-btn tr-btn--danger" (click)="delOpen.set(true)">Delete website</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <mo-dialog [open]="delOpen()" title="Delete website?" (closed)="delOpen.set(false)">
        <div style="max-width:380px;display:flex;flex-direction:column;gap:14px">
          <p style="margin:0;font-size:13.5px;line-height:1.55">This permanently deletes <strong>hazeliscoding.com</strong> and every aggregate Mochi has stored for it. There is no undo and no recovery window.</p>
          <div>
            <label class="tr-label" for="del-confirm">Type the domain to confirm</label>
            <input id="del-confirm" type="text" class="tr-input" placeholder="hazeliscoding.com" />
          </div>
          <div style="display:flex;justify-content:flex-end;gap:8px">
            <button type="button" class="tr-btn tr-btn--secondary" (click)="delOpen.set(false)">Cancel</button>
            <button type="button" class="tr-btn tr-btn--danger" (click)="delOpen.set(false)">Delete website</button>
          </div>
        </div>
      </mo-dialog>
    </section>
  `,
})
export class Settings {
  protected readonly data = inject(AnalyticsDataService);

  protected readonly tz = signal('(UTC−05:00) Eastern Time');
  protected readonly qsIgnore = signal(true);
  protected readonly botFilter = signal(true);
  protected readonly selfFilter = signal(false);
  protected readonly geoPrecision = signal('Country only');
  protected readonly geoOptions = ['Country only', 'Country + region (thresholded)'];
  protected readonly delOpen = signal(false);

  protected sel(e: Event): string {
    return (e.target as HTMLSelectElement).value;
  }

  protected chk(e: Event): boolean {
    return (e.target as HTMLInputElement).checked;
  }
}
