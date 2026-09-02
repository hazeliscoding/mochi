import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

@Component({
  selector: 'mo-code-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tr-code">
      <header>
        <span>{{ filename() }}</span>
        <button
          type="button"
          class="tr-btn tr-btn--ghost tr-btn--sm"
          style="height:22px"
          (click)="copy()"
        >
          {{ copied() ? 'Copied' : 'Copy' }}
        </button>
      </header>
      <pre><code>{{ code() }}</code></pre>
    </div>
  `,
})
export class CodeBlock {
  readonly filename = input<string>();
  readonly code = input.required<string>();
  readonly copied = signal(false);

  copy(): void {
    navigator.clipboard?.writeText(this.code());
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 1500);
  }
}
