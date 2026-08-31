import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { BarRow } from '../core/analytics-data.service';

/** Rows with a proportional accent-tint fill behind name/pct/value. */
@Component({
  selector: 'mo-bar-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (r of rows(); track r.name) {
      <div class="mo-bar-row">
        <div class="mo-bar-row__fill" [style.width.%]="r.pct"></div>
        <span class="mo-bar-row__name" [class.mo-mono]="mono()" [style.font-size]="mono() ? '12.5px' : null">{{ r.name }}</span>
        <span class="mo-bar-row__vals">
          @if (showPct()) {
            <span class="mo-bar-row__pct">{{ r.pct }}%</span>
          }
          <span class="mo-bar-row__val" [style.min-width.px]="valWidth()">{{ r.val }}</span>
        </span>
      </div>
    }
  `,
})
export class BarList {
  readonly rows = input.required<BarRow[]>();
  readonly showPct = input(true);
  readonly mono = input(false);
  readonly valWidth = input(44);
}
