import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { CurrentUser } from '../api/current-user';
import { Leaderboard, LeaderboardEntry, Sport } from '../api/models';
import { LogActivity } from './log-activity';

const USER = '11111111-1111-1111-1111-111111111111';

const SPORTS: Sport[] = [
  { sport: 'running', label: 'Running', metric: 'distance', unit: 'km', pointsPerUnit: 100 },
  { sport: 'swimming', label: 'Swimming', metric: 'duration', unit: 'mm:ss', pointsPerUnit: 15 },
  { sport: null, label: 'Daily steps', metric: 'count', unit: 'steps', pointsPerUnit: 0.01 },
];

const RIVAL = '33333333-3333-3333-3333-333333333333';

function entry(rank: number, userId: string, firstName: string, totalPoints: number): LeaderboardEntry {
  return {
    rank, userId, firstName, lastName: 'Miller', totalPoints,
    rankDelta: null, pointsToOvertake: rank === 1 ? null : 1,
  };
}

const board = (mine: number): Leaderboard => ({
  challenge: null,
  activeToday: 0,
  entries: mine === 1
    ? [entry(1, USER, 'Anna', 900), entry(2, RIVAL, 'Sarah', 800)]
    : [entry(1, RIVAL, 'Sarah', 800), entry(2, USER, 'Anna', 500)],
});

async function open(
  sport?: string,
): Promise<{ fixture: ComponentFixture<LogActivity>; http: HttpTestingController }> {
  await TestBed.configureTestingModule({
    imports: [LogActivity],
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      {

        provide: ActivatedRoute,
        useValue: {
          snapshot: { queryParamMap: convertToParamMap(sport === undefined ? {} : { sport }) },
        },
      },
    ],
  }).compileComponents();

  TestBed.inject(CurrentUser).select(USER);

  const fixture = TestBed.createComponent(LogActivity);
  fixture.detectChanges();

  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/sports').flush(SPORTS);
  http.expectOne('/api/leaderboard').flush(board(2));
  fixture.detectChanges();

  return { fixture, http };
}

const bodyOf = (http: HttpTestingController) =>
  http.expectOne('/api/activities').request.body as Record<string, unknown>;

describe('LogActivity', () => {

  it('asks for the measurement the chosen sport is scored on', async () => {
    const { fixture } = await open();
    const form = fixture.componentInstance;

    expect(fixture.nativeElement.textContent).toContain('100 points per km');

    form.selectedKey.set('swimming');
    fixture.detectChanges();

    expect(form.selected()?.metric).toBe('duration');
    expect(fixture.nativeElement.textContent).toContain('15 points per mm:ss');
  });

  it('sends the field that sport is scored on, not the one the form opened with', async () => {
    const { fixture, http } = await open();
    const form = fixture.componentInstance;

    form.selectedKey.set('swimming');
    form.amount.set('31:20');
    form.submit();

    const body = bodyOf(http);

    expect(body['sport']).toBe('swimming');
    expect(body['duration']).toBe('31:20');
    expect(body['distance']).toBeUndefined();
  });

  it('omits the sport entirely for daily steps', async () => {
    const { fixture, http } = await open();
    const form = fixture.componentInstance;

    form.selectedKey.set('');
    form.amount.set('8000');
    form.submit();

    const body = bodyOf(http);

    expect('sport' in body).toBe(false);
    expect(body['steps']).toBe(8000);
  });

  it('opens on the sport the coach recommended', async () => {
    const { fixture } = await open('swimming');

    expect(fixture.componentInstance.selected()?.label).toBe('Swimming');
    expect(fixture.nativeElement.textContent).toContain('15 points per mm:ss');
  });

  it('treats an empty sport parameter as daily steps', async () => {
    const { fixture } = await open('');

    expect(fixture.componentInstance.selected()?.label).toBe('Daily steps');
  });

  it('falls back to running when the parameter names no sport it has', async () => {
    const { fixture } = await open('quidditch');

    expect(fixture.componentInstance.selected()?.label).toBe('Running');
  });

  it('reports the place the activity won and who it got past', async () => {
    const { fixture, http } = await open();
    const form = fixture.componentInstance;

    form.amount.set('5');
    form.submit();

    http.expectOne('/api/activities').flush({ points: 500 });
    http.expectOne('/api/leaderboard').flush(board(1));
    fixture.detectChanges();

    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('+500 points');
    expect(text).toContain('#2');
    expect(text).toContain('#1');
    expect(text).toContain('You overtook Sarah Miller.');
  });

  it('says so plainly when the activity did not change the ranking', async () => {
    const { fixture, http } = await open();
    const form = fixture.componentInstance;

    form.amount.set('0.1');
    form.submit();

    http.expectOne('/api/activities').flush({ points: 10 });
    http.expectOne('/api/leaderboard').flush(board(2));
    fixture.detectChanges();

    expect(fixture.componentInstance.move()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Your place is unchanged');
  });

  it('previews what the activity would score, using the same rules the server does', async () => {
    const { fixture } = await open();
    const form = fixture.componentInstance;

    form.amount.set('4.2');
    fixture.detectChanges();
    expect(form.preview()).toBe(420);

    form.amount.set('0.299');
    expect(form.preview()).toBe(29);

    form.selectedKey.set('swimming');
    form.amount.set('1:55');
    expect(form.preview()).toBe(15);

    form.amount.set('1:75');
    expect(form.preview()).toBeNull();

    form.selectedKey.set('');
    form.amount.set('399');
    expect(form.preview()).toBe(3);
  });
});
