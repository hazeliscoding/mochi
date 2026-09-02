import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Privacy } from './privacy';

const SITE = { id: 'MC-1', name: 'demo', domain: 'demo.test', timezone: 'UTC', retention: '90d', snippet: '' };

describe('Privacy', () => {
  let fixture: ComponentFixture<Privacy>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Privacy],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(Privacy);
    http = TestBed.inject(HttpTestingController);
  });

  // whenStable would wait on stats resources this page never answers, so the
  // helper drives effects with a task hop plus TestBed.tick() and answers only
  // its own calls.
  async function settle(): Promise<void> {
    await new Promise(resolve => setTimeout(resolve));
    TestBed.tick();
  }

  async function flushHappyPath(rawEventsHeld: number, oldest: string | null): Promise<void> {
    fixture.detectChanges();
    await settle();
    http.expectOne('/api/sites').flush([{ site: SITE, viewsLast30d: 1, activeNow: 0, status: 'active' }]);
    await settle();
    http.expectOne('/api/sites/MC-1/privacy').flush({
      retention: '90d',
      rawEventLifetimeDays: 7,
      rawEventsHeld,
      oldestAggregateDate: oldest,
    });
    await settle();
    fixture.detectChanges();
  }

  it('renders the live holdings from the privacy endpoint', async () => {
    await flushHappyPath(1234, '2026-08-01');
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('1,234');
    expect(text).toContain('deleted 7 days after');
    expect(text).toContain('Aug 1, 2026');
    expect(text).toContain('90 days');
  });

  it('shows a placeholder before the first rollup', async () => {
    await flushHappyPath(5, null);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No aggregates yet');
  });

  it('persists a retention change via PUT and shows Saved', async () => {
    await flushHappyPath(5, '2026-08-01');
    const radios = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement>('input[type="radio"]');
    radios[0].click();

    const put = http.expectOne(r => r.method === 'PUT' && r.url === '/api/sites/MC-1');
    expect(put.request.body).toEqual({ retention: '30d' });
    put.flush({ ...SITE, retention: '30d' });
    await settle();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Saved.');
  });
});
