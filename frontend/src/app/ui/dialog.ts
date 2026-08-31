import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'mo-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'open() && closed.emit()' },
  template: `
    @if (open()) {
      <div class="tr-scrim" style="display:flex;align-items:center;justify-content:center" (mousedown)="onScrim($event)">
        <div class="tr-dialog" role="dialog" aria-modal="true" [attr.aria-label]="title()" [style.width]="width()">
          @if (title()) {
            <header><h2>{{ title() }}</h2></header>
          }
          <div class="tr-dialog__body">
            <ng-content />
          </div>
        </div>
      </div>
    }
  `,
})
export class Dialog {
  readonly open = input.required<boolean>();
  readonly title = input<string>();
  readonly width = input<string>();
  readonly closed = output<void>();

  onScrim(e: MouseEvent): void {
    if (e.target === e.currentTarget) this.closed.emit();
  }
}
