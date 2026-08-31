import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ICONS } from './icons';

/** Lucide icon renderer (stroke icons, 1.75px weight per the Trellis spec). */
@Component({
  selector: 'mo-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display:inline-flex;line-height:0' },
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      [attr.stroke-width]="strokeWidth()"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
      [innerHTML]="inner()"
    ></svg>
  `,
})
export class Icon {
  readonly name = input.required<string>();
  readonly size = input(16);
  readonly strokeWidth = input(1.75);

  private readonly sanitizer = inject(DomSanitizer);

  readonly inner = computed<SafeHtml>(() =>
    this.sanitizer.bypassSecurityTrustHtml(ICONS[this.name()] ?? ''),
  );
}
