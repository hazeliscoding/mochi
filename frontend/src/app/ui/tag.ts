import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'mo-tag',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'cls()' },
  template: `<ng-content />`,
})
export class Tag {
  readonly tone = input<'accent' | 'info' | 'success' | 'warning' | 'danger' | undefined>(undefined);
  readonly cls = computed(() => 'tr-tag' + (this.tone() ? ' tr-tag--' + this.tone() : ''));
}
