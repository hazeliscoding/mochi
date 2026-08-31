import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export interface ButtonGroupItem {
  value: string;
  label: string;
}

@Component({
  selector: 'mo-button-group',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tr-btngroup" role="group">
      @for (it of items(); track it.value) {
        <button
          type="button"
          class="tr-btn tr-btn--secondary tr-btn--sm"
          [attr.aria-pressed]="value() === it.value"
          (click)="valueChange.emit(it.value)"
        >
          {{ it.label }}
        </button>
      }
    </div>
  `,
})
export class ButtonGroup {
  readonly items = input.required<ButtonGroupItem[]>();
  readonly value = input.required<string>();
  readonly valueChange = output<string>();
}
