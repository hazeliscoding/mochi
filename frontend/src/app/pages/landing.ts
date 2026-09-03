import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { ThemeService } from '../core/theme.service';
import { Icon } from '../ui/icon';

const REPO = 'https://github.com/hazeliscoding/mochi';

/** Public marketing page at the root URL. No guard, no dashboard data. */
@Component({
  selector: 'mo-landing',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Icon],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing {
  protected readonly theme = inject(ThemeService);

  protected readonly repo = REPO;
  protected readonly release = `${REPO}/releases/tag/v1.0.0`;

  /** False until /api/auth/status resolves; anonymous visitors see "Sign in". */
  protected readonly authed = signal(false);

  protected readonly shot = computed(() =>
    this.theme.theme() === 'dark' ? '/landing/overview.png' : '/landing/overview-light.png',
  );

  constructor() {
    // A failed status call keeps the anonymous "Sign in" default.
    void inject(AuthService)
      .ensureStatus()
      .then(
        (s) => this.authed.set(s.authenticated),
        () => {},
      );
  }

  /** Feature grid: icon, title, one line. All claims come from the README. */
  protected readonly features: ReadonlyArray<[string, string, string]> = [
    [
      'layout-dashboard',
      'The full dashboard',
      'Trends, comparison periods, entry and exit pages, bounce rates.',
    ],
    [
      'radio-tower',
      'Realtime',
      'Active visits in the last 5 minutes with a per-minute pageview chart.',
    ],
    ['compass', 'Traffic sources', 'Channels, referrers and campaigns, plus geography and devices.'],
    [
      'target',
      'Goals',
      'Conversions computed at query time. A goal created today shows its full history.',
    ],
    [
      'mouse-pointer-click',
      'Custom events',
      "mochi('event', 'signup') with per-page breakdowns.",
    ],
    [
      'layers',
      'A 1.9 KB snippet',
      'Auto pageviews, SPA route detection, and it bails out entirely on DNT and Global Privacy Control.',
    ],
    ['download', 'Your data, portable', 'One-click export of everything Mochi holds as CSVs.'],
    [
      'shield-check',
      'Retention that purges',
      'Keep aggregates 30 days to unlimited. Deleting a site cascades everything.',
    ],
  ];

  /** Three-step "how it works" strip. */
  protected readonly steps: ReadonlyArray<[string, string]> = [
    [
      'Paste the snippet',
      '1.9 KB, no cookies, no client-side storage. Visitors with Do Not Track or Global Privacy Control are never counted.',
    ],
    [
      'Scrubbed at ingest',
      'The IP becomes a country and a hash input, then it is discarded. The user agent is reduced to browser, OS and device class, no versions kept.',
    ],
    [
      'Aggregates only',
      'Raw events live 7 days. A nightly job rolls them into daily aggregate tables that hold counts, never per-visit rows.',
    ],
  ];
}
