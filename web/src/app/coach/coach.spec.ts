import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CurrentUser } from '../api/current-user';
import { CoachFacts } from '../api/models';
import { Coach } from './coach';

const USER = '11111111-1111-1111-1111-111111111111';

const facts: CoachFacts = {
  userId: USER,
  challenge: {
    name: 'Move More',
    startsOn: '2026-08-17',
    endsOn: '2026-09-15',
    day: 24,
    totalDays: 30,
    daysLeft: 6,
    hasEnded: false,
  },
  rank: 2,
  competitors: 5,
  totalPoints: 300,
  pointsToPassNextRank: 201,
  pointsToTakeTheLead: 201,
  rankChange: null,
  outlook: null,
  activeDaysLastSevenDays: 5,
  bestDay: null,
  leadOverNextRank: null,
  nextRankName: 'Sarah Miller',
  activitiesLastSevenDays: 2,
  pointsLastSevenDays: 300,
  averageWeeklyPoints: 260,
  daysTrainedLastFourWeeks: 9,
  bestWeekday: 'Tuesday',
  waysToPassNextRank: [
    { sport: 'running', label: 'Running', quantity: 2.01, unit: 'km' },
    { sport: 'walking', label: 'Walking', quantity: 3.48, unit: 'km' },
    { sport: null, label: 'Daily steps', quantity: 20100, unit: 'steps' },
  ],
  recommended: { sport: 'running', label: 'Running', quantity: 2.01, unit: 'km' },
  sports: [
    { label: 'Running', points: 300, sessions: 4, averagePoints: 75 },
    { label: 'Walking', points: 120, sessions: 2, averagePoints: 60 },
    { label: 'Swimming', points: 0, sessions: 0, averagePoints: 0 },
  ],
};

async function open(body: CoachFacts = facts): Promise<{ fixture: ComponentFixture<Coach>; http: HttpTestingController }> {
  await TestBed.configureTestingModule({
    imports: [Coach],
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  }).compileComponents();

  TestBed.inject(CurrentUser).select(USER);

  const fixture = TestBed.createComponent(Coach);
  fixture.detectChanges();

  const http = TestBed.inject(HttpTestingController);
  http.expectOne(`/api/users/${USER}/coach`).flush(body);
  fixture.detectChanges();

  return { fixture, http };
}

describe('Coach', () => {
  it('opens on the challenge, the person ahead and the one way that fits them', async () => {
    const { fixture } = await open();
    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('Move More');
    expect(text).toContain('day 24 of 30');
    expect(text).toContain('6 days left');

    const headline: string = fixture.nativeElement.querySelector('.brief h2').textContent;
    expect(headline).toContain('Sarah Miller is 201 points ahead.');
    expect(headline).not.toContain('#2');

    expect(text).toContain('You do running more than anything else');
    expect(text).toContain('Ranked #2 of 5.');
    expect(text).toContain('Tuesdays');

    expect(text).not.toContain('Daily steps');
  });

  it('sends the recommendation straight to the log form', async () => {
    const { fixture } = await open();
    const cta: HTMLAnchorElement = fixture.nativeElement.querySelector('.move .btn-primary');

    expect(cta.textContent!.trim()).toBe('Log running →');
    expect(cta.getAttribute('href')).toBe('/log?sport=running');
  });

  it('builds the recommended move from the computed figures, not from the reply', async () => {
    const { fixture, http } = await open();

    fixture.componentInstance.send('How do I catch up?');
    http.expectOne(`/api/users/${USER}/coach/chat`).flush({ reply: 'Run 99 km to take rank 1.' });
    fixture.detectChanges();

    const card: string = fixture.nativeElement.querySelector('.move').textContent;

    expect(card).toContain('2.01');
    expect(card).toContain('closes the current gap to rank 1');
    expect(card).not.toContain('99');
  });

  it('offers the next best sport they actually do, as a choice rather than an order', async () => {
    const { fixture } = await open();

    expect(fixture.componentInstance.alternative()?.label).toBe('Walking');
    expect(fixture.nativeElement.querySelector('.other').textContent).toContain('3.48');
  });

  it('sends the whole conversation and threads the reply', async () => {
    const { fixture, http } = await open();
    const coach = fixture.componentInstance;

    coach.send('Plan my week');

    const request = http.expectOne(`/api/users/${USER}/coach/chat`);
    expect(request.request.body).toEqual({ messages: [{ role: 'user', text: 'Plan my week' }] });

    request.flush({ reply: 'Run 2.01 km on Tuesday.' });
    fixture.detectChanges();

    expect(coach.messages()).toEqual([
      { role: 'user', text: 'Plan my week' },
      { role: 'assistant', text: 'Run 2.01 km on Tuesday.' },
    ]);
    expect(fixture.nativeElement.textContent).toContain('Run 2.01 km on Tuesday.');
  });

  it('says the coach is switched off without credentials, and keeps the figures', async () => {
    const { fixture, http } = await open();

    fixture.componentInstance.send('How do I catch up?');
    http
      .expectOne(`/api/users/${USER}/coach/chat`)
      .flush({ title: 'The AI coach is not configured.' }, { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();

    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('switched off');
    expect(text).toContain('2.01');
  });

  it('hands an unanswered question back instead of leaving it in the thread', async () => {
    const { fixture, http } = await open();
    const coach = fixture.componentInstance;

    coach.send('Plan my week');
    http.expectOne(`/api/users/${USER}/coach/chat`).flush(null, { status: 502, statusText: 'Bad Gateway' });
    fixture.detectChanges();

    expect(coach.messages()).toEqual([]);
    expect(coach.draft()).toBe('Plan my week');
  });

  it('gives the leader something to do, and prompts that fit being first', async () => {
    const leader: CoachFacts = {
      ...facts,
      rank: 1,
      pointsToPassNextRank: null,
      pointsToTakeTheLead: null,
      nextRankName: null,
      leadOverNextRank: 6299,
      recommended: null,
    };

    const { fixture } = await open(leader);
    const card: string = fixture.nativeElement.querySelector('.move').textContent;

    expect(card).toContain('Protect your lead');
    expect(card).toContain('6,299');
    expect(fixture.nativeElement.querySelector('.move .btn-primary').getAttribute('href')).toBe('/log');

    const prompts = fixture.componentInstance.suggestions();
    expect(prompts).toContain('How do I stay ahead?');
    expect(prompts).not.toContain('How do I catch up?');
    expect(prompts).not.toContain('Can I reach #1?');
  });

  it('keeps offering questions after a reply, without repeating one already asked', async () => {
    const { fixture, http } = await open();
    const coach = fixture.componentInstance;

    expect(coach.suggestions()).toContain('How do I catch up?');

    coach.send('How do I catch up?');
    http.expectOne(`/api/users/${USER}/coach/chat`).flush({ reply: 'Run 2.01 km.' });
    fixture.detectChanges();

    const left = coach.suggestions();

    expect(left).not.toContain('How do I catch up?');
    expect(left.length).toBeGreaterThan(0);

    expect(fixture.nativeElement.querySelectorAll('.starters button').length).toBe(left.length);
  });
});
