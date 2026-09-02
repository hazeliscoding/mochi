import { Component, ElementRef, inject, viewChild } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AnalyticsDataService } from './core/analytics-data.service';
import { ThemeService } from './core/theme.service';
import { Icon } from './ui/icon';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon],
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly data = inject(AnalyticsDataService);
  protected readonly theme = inject(ThemeService);
  private readonly router = inject(Router);
  private readonly main = viewChild<ElementRef<HTMLElement>>('main');

  protected readonly analyticsNav: NavItem[] = [
    { path: '/overview', label: 'Overview', icon: 'layout-dashboard' },
    { path: '/realtime', label: 'Realtime', icon: 'activity' },
    { path: '/pages', label: 'Pages', icon: 'file-text' },
    { path: '/sources', label: 'Sources', icon: 'compass' },
    { path: '/geography', label: 'Geography', icon: 'globe' },
    { path: '/devices', label: 'Devices', icon: 'monitor-smartphone' },
    { path: '/events', label: 'Events', icon: 'mouse-pointer-click' },
    { path: '/goals', label: 'Goals', icon: 'target' },
  ];

  protected readonly manageNav: NavItem[] = [
    { path: '/websites', label: 'Websites', icon: 'layers' },
    { path: '/add-website', label: 'Add website', icon: 'plus' },
    { path: '/privacy', label: 'Privacy center', icon: 'shield-check' },
    { path: '/settings', label: 'Website settings', icon: 'settings' },
  ];

  protected readonly allNav = [...this.analyticsNav, ...this.manageNav];

  protected readonly siteOptions = this.data.siteOptions;

  constructor() {
    this.router.events.subscribe(e => {
      if (e instanceof NavigationEnd) this.main()?.nativeElement.scrollTo({ top: 0 });
    });
  }

  protected onSelect(signalTarget: 'site' | 'range' | 'compare', e: Event): void {
    const value = (e.target as HTMLSelectElement).value;
    if (signalTarget === 'site') this.data.siteId.set(value);
    else if (signalTarget === 'range') this.data.range.set(value);
    else this.data.compare.set(value);
  }
}
