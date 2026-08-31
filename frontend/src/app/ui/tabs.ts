import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export interface TabItem {
  value: string;
  label: string;
  count?: number;
}

@Component({
  selector: 'mo-tabs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tr-tabs" role="tablist">
      @for (t of tabs(); track t.value) {
        <button
          type="button"
          role="tab"
          class="tr-tab"
          [attr.aria-selected]="value() === t.value"
          (click)="valueChange.emit(t.value)"
        >
          {{ t.label }}
          @if (t.count != null) {
            <span style="margin-left:6px;font:var(--type-caption);color:var(--color-text-disabled)">{{ t.count }}</span>
          }
        </button>
      }
    </div>
  `,
})
export class Tabs {
  readonly tabs = input.required<TabItem[]>();
  readonly value = input.required<string>();
  readonly valueChange = output<string>();
}
