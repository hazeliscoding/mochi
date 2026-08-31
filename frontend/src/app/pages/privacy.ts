import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AnalyticsDataService } from '../core/analytics-data.service';

@Component({
  selector: 'mo-privacy',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section style="max-width:760px">
      <h1 class="mo-page-title" style="margin:4px 0 6px">Privacy center</h1>
      <div style="font-size:14px;color:var(--color-text-secondary);margin-bottom:22px;max-width:560px">Mochi measures your website, not your visitors. Here is exactly what that means — no legal document required.</div>

      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(230px,1fr));gap:10px;margin-bottom:26px">
        @for (c of data.privChecks; track c[0]) {
          <div class="mo-card" style="display:flex;gap:10px;align-items:flex-start;padding:12px 14px">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--color-success)" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="flex:none;margin-top:2px" aria-hidden="true"><path d="M20 6 9 17l-5-5" /></svg>
            <span><span style="font-weight:600;display:block">{{ c[0] }}</span><span style="font-size:12.5px;color:var(--color-text-secondary)">{{ c[1] }}</span></span>
          </div>
        }
      </div>

      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:16px;margin-bottom:26px">
        <div class="mo-card" style="padding:16px 18px">
          <div class="mo-card-label" style="margin-bottom:10px">Data we collect</div>
          @for (i of data.collectedItems; track i[0]) {
            <div style="padding:5px 0;font-size:13.5px;border-bottom:1px solid var(--color-border-subtle)">{{ i[0] }} <span style="color:var(--color-text-secondary);font-size:12.5px">— {{ i[1] }}</span></div>
          }
        </div>
        <div class="mo-card" style="padding:16px 18px">
          <div class="mo-card-label" style="margin-bottom:10px">Data we refuse to collect</div>
          @for (i of data.notCollectedItems; track i[0]) {
            <div style="padding:5px 0;font-size:13.5px;border-bottom:1px solid var(--color-border-subtle);display:flex;gap:8px"><span style="color:var(--color-text-disabled)" aria-hidden="true">✕</span><span>{{ i[0] }} <span style="color:var(--color-text-secondary);font-size:12.5px">— {{ i[1] }}</span></span></div>
          }
        </div>
      </div>

      <div class="mo-card" style="padding:16px 18px;margin-bottom:16px">
        <div class="mo-card-label" style="margin-bottom:4px">Data retention</div>
        <div style="font-size:13px;color:var(--color-text-secondary);margin-bottom:12px">How long Mochi keeps daily aggregates for this website. Nothing here extends what is collected — only how long totals are kept.</div>
        <div style="display:flex;flex-direction:column;gap:8px">
          @for (r of data.retentionOptions; track r[0]) {
            <label style="display:flex;gap:10px;align-items:center;cursor:pointer">
              <input type="radio" name="retention" [checked]="data.retention() === r[0]" (change)="data.retention.set(r[0])" style="accent-color:var(--color-accent)" />
              <span style="font-weight:600;min-width:190px">{{ r[0] }}</span>
              <span style="font-size:12.5px;color:var(--color-text-secondary)">{{ r[1] }}</span>
            </label>
          }
        </div>
      </div>

      <div class="mo-card" style="padding:16px 18px">
        <div class="mo-card-label" style="margin-bottom:8px">Privacy thresholds</div>
        <p style="margin:0;font-size:13.5px;line-height:1.55;max-width:560px">When a group gets very small — say, three visitors from one region on one browser — reporting it could identify someone. Mochi automatically groups or hides segments below a safe minimum, so your reports stay useful without ever pointing at a person.</p>
      </div>
    </section>
  `,
})
export class Privacy {
  protected readonly data = inject(AnalyticsDataService);
}
