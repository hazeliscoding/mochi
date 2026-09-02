import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'mo-metric',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tr-metric__label">{{ label() }}</div>
    <div class="tr-metric__value">{{ value() }}</div>
    @if (delta()) {
      <div
        class="tr-metric__delta"
        [class.tr-metric__delta--up]="dir() === 'up'"
        [class.tr-metric__delta--down]="dir() === 'down'"
      >
        {{ delta() }}
      </div>
    }
  `,
})
export class Metric {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly delta = input<string>();
  readonly dir = input<'up' | 'down'>();
}
