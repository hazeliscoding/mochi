import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Full-width card for loading, error and no-data states. */
@Component({
  selector: 'mo-page-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mo-card" style="padding:44px 24px;text-align:center;color:var(--color-text-secondary);font-size:13.5px">
      @switch (kind()) {
        @case ('loading') {
          <span>Loading analytics…</span>
        }
        @case ('error') {
          <span style="display:block;font-weight:600;color:var(--color-text-primary);margin-bottom:4px">Could not reach the Mochi API</span>
          <span>Check that the backend is running, then reload.</span>
        }
        @case ('empty') {
          <span style="display:block;font-weight:600;color:var(--color-text-primary);margin-bottom:4px">No data yet</span>
          <span>Install your snippet and visits will show up here within seconds.</span>
        }
      }
    </div>
  `,
})
export class PageState {
  // 'ready' renders nothing; callers can pass a FetchState straight through.
  readonly kind = input.required<'loading' | 'error' | 'empty' | 'ready'>();
}
