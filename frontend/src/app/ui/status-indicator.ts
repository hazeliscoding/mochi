import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'mo-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'cls()' },
  template: `<ng-content />`,
})
export class StatusIndicator {
  readonly tone = input<'success' | 'warning' | 'danger' | 'info' | 'neutral'>('neutral');
  readonly cls = computed(() => 'tr-status' + (this.tone() !== 'neutral' ? ' tr-status--' + this.tone() : ''));
}
