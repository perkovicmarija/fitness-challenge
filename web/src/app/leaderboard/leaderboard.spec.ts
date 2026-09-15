import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CurrentUser } from '../api/current-user';
import { Leaderboard as Board, ChallengeSummary, LeaderboardEntry } from '../api/models';
import { Leaderboard } from './leaderboard';

const ME = '22222222-2222-2222-2222-222222222222';

function entry(
  rank: number,
  userId: string,
  firstName: string,
  totalPoints: number,
  pointsToOvertake: number | null,
  surname = 'Wilson',
): LeaderboardEntry {
  return {
    rank, userId, firstName, lastName: surname, totalPoints,
    rankDelta: null, pointsToOvertake,
  };
}

const challenge: ChallengeSummary = {
  name: 'Move More',
  startsOn: '2026-08-16',
  endsOn: '2026-09-14',
  day: 24,
  totalDays: 30,
  daysLeft: 6,
  hasEnded: false,
};

const board: Board = {
  challenge: null,
  activeToday: 3,
  entries: [
    entry(1, '11111111-1111-1111-1111-111111111111', 'Anna', 500, null),
    entry(2, ME, 'Emma', 440, 61, 'Davis'),
    entry(3, '33333333-3333-3333-3333-333333333333', 'Alex', 200, 241, 'Martin'),
  ],
};

async function open(
  selected: string | null,
  body: Board = board,
): Promise<ComponentFixture<Leaderboard>> {
  await TestBed.configureTestingModule({
    imports: [Leaderboard],
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  }).compileComponents();

  TestBed.inject(CurrentUser).select(selected);

  const fixture = TestBed.createComponent(Leaderboard);
  fixture.detectChanges();

  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/leaderboard').flush(body);

  if (selected) {
    http.expectOne(`/api/users/${selected}/coach`).flush({ rankChange: null });
  }
  fixture.detectChanges();

  return fixture;
}

describe('Leaderboard', () => {
  it('names who the viewer is chasing and links to the ways to catch them', async () => {
    const fixture = await open(ME);
    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('#2');

    expect(text).toContain('Take the lead');
    expect(text).toContain('61 pts to overtake Anna Wilson.');

    expect(text).toContain('61');

    const cta: HTMLAnchorElement = fixture.nativeElement.querySelector('.cta');
    expect(cta.getAttribute('href')).toBe('/coach');
  });

  it('tells the leader they are leading rather than showing a gap', async () => {
    const fixture = await open('11111111-1111-1111-1111-111111111111');

    expect(fixture.nativeElement.textContent).toContain('Protect your lead');
    expect(fixture.nativeElement.textContent).toContain('You lead Emma by 60 pts.');
    expect(fixture.nativeElement.querySelector('.cta')).toBeNull();
  });

  it('asks for a person when nobody is selected', async () => {
    const fixture = await open(null);

    expect(fixture.nativeElement.textContent).toContain('Choose a person above');
  });

  it('gives one person the same badge colour every time', async () => {
    const first = (await open(ME)).componentInstance.rows();
    TestBed.resetTestingModule();
    const second = (await open(ME)).componentInstance.rows();

    expect(first.map(row => row.badge)).toEqual(second.map(row => row.badge));
    expect(first.map(row => row.initials)).toEqual(['AW', 'ED', 'AM']);
  });

  it('gives the ranking a deadline to sit against', async () => {
    const fixture = await open(ME, { ...board, challenge });
    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('Move More');
    expect(text).toContain('Day 24 of 30');
    expect(text).toContain('Live');
    expect(text).toContain('3 participants');
    expect(text).toContain('6 days left');

    expect(fixture.componentInstance.percent()).toBe(80);
  });

  it('still ranks when there is no challenge to sit against', async () => {
    const fixture = await open(ME);

    expect(fixture.nativeElement.textContent).not.toContain('Move More');
    expect(fixture.nativeElement.querySelector('[role="progressbar"]')).toBeNull();
    expect(fixture.componentInstance.rows().length).toBe(3);
  });

  it('shows the places either side of the viewer as a race', async () => {
    const fixture = await open(ME, { ...board, challenge });
    const race = fixture.componentInstance.race();

    expect(race.map(row => row.rank)).toEqual([1, 2, 3]);

    const text: string = fixture.nativeElement.querySelector('.race').textContent;
    expect(text).toContain('61 pts');
    expect(text).toContain('241 pts');
  });

  it('reads the challenge state off the figures rather than inventing one', async () => {
    const live = await open(ME, { ...board, challenge });
    expect(live.componentInstance.state()).toBe('live');

    TestBed.resetTestingModule();
    const final = await open(ME, { ...board, challenge: { ...challenge, daysLeft: 0 } });
    expect(final.componentInstance.state()).toBe('final');
    expect(final.nativeElement.textContent).toContain('Final day');

    TestBed.resetTestingModule();
    const done = await open(ME, { ...board, challenge: { ...challenge, hasEnded: true } });
    expect(done.componentInstance.state()).toBe('complete');
    expect(done.nativeElement.textContent).toContain('Final rank');

    TestBed.resetTestingModule();
    const soon = await open(ME, { ...board, challenge: { ...challenge, day: 0 } });
    expect(soon.componentInstance.state()).toBe('upcoming');
  });

  it('leaves the rows around the viewer to the race block', async () => {
    const fixture = await open(ME, { ...board, challenge });
    const gaps = fixture.componentInstance.rows().map(row => row.gap?.text ?? null);

    expect(gaps).toEqual([null, null, null]);
  });

  it('keeps the gap on rows the race block does not reach', async () => {
    const far: Board = {
      ...board,
      entries: [
        entry(1, '11111111-1111-1111-1111-111111111111', 'Anna', 900, null),
        entry(2, '44444444-4444-4444-4444-444444444444', 'Alex', 700, 201),
        entry(3, ME, 'Emma', 500, 201),
      ],
    };

    const fixture = await open(ME, far);
    const gaps = fixture.componentInstance.rows().map(row => row.gap?.text ?? null);

    expect(gaps[0]).toBe('401 pts to #1');
    expect(gaps[1]).toBeNull();
    expect(gaps[2]).toBeNull();
  });

  it('asks for the explanation only when the panel is opened', async () => {
    await TestBed.configureTestingModule({
      imports: [Leaderboard],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    TestBed.inject(CurrentUser).select(ME);

    const fixture = TestBed.createComponent(Leaderboard);
    fixture.detectChanges();

    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/leaderboard').flush({ ...board, challenge });
    http.expectOne(`/api/users/${ME}/coach`).flush({
      rankChange: { previousRank: 1, currentRank: 2, myPointsThisWeek: 340, mover: 'Sarah Miller', moverPointsThisWeek: 620 },
    });
    fixture.detectChanges();

    http.expectNone(`/api/users/${ME}/coach/insight/rankChange`);
    expect(fixture.nativeElement.textContent).toContain('#1 → #2 this week');

    fixture.componentInstance.explain();
    fixture.detectChanges();

    const panel: string = fixture.nativeElement.querySelector('.panel').textContent;
    expect(panel).toContain('+340');
    expect(panel).toContain('+620');
    expect(panel).toContain('-280');

    http.expectOne(`/api/users/${ME}/coach/insight/rankChange`).flush({ reply: 'Sarah out-earned you by 280 points.' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Sarah out-earned you by 280 points.');
  });

  it('keeps the figures when the explanation cannot be fetched', async () => {
    await TestBed.configureTestingModule({
      imports: [Leaderboard],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    TestBed.inject(CurrentUser).select(ME);

    const fixture = TestBed.createComponent(Leaderboard);
    fixture.detectChanges();

    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/leaderboard').flush({ ...board, challenge });
    http.expectOne(`/api/users/${ME}/coach`).flush({
      rankChange: { previousRank: 1, currentRank: 2, myPointsThisWeek: 340, mover: 'Sarah Miller', moverPointsThisWeek: 620 },
    });
    fixture.detectChanges();

    fixture.componentInstance.explain();
    http.expectOne(`/api/users/${ME}/coach/insight/rankChange`)
      .flush(null, { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();

    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('temporarily unavailable');
    expect(text).toContain('+340');
    expect(text).toContain('61');
  });
});
