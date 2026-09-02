import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { sparkD } from '../core/chart';

@Component({
  selector: 'mo-sparkline',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display:inline-flex;line-height:0' },
  template: `
    <svg
      [attr.viewBox]="'0 0 ' + width() + ' ' + height()"
      [style.width.px]="width()"
      [style.height.px]="height()"
      aria-hidden="true"
    >
      <path [attr.d]="d()" fill="none" stroke="var(--color-accent)" stroke-width="1.5" />
    </svg>
  `,
})
export class Sparkline {
  readonly data = input<number[]>();
  readonly path = input<string>();
  readonly width = input(90);
  readonly height = input(24);

  readonly d = computed(() => {
    const p = this.path();
    if (p) return p;
    const arr = this.data();
    return arr && arr.length ? sparkD(arr, this.width(), this.height()) : '';
  });
}
