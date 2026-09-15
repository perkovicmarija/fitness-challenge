import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Api } from '../api/api';
import { CurrentUser } from '../api/current-user';
import { ChallengeSummary, LeaderboardEntry, RankChange } from '../api/models';

const BadgeColours = 6;

interface Row extends LeaderboardEntry {
  initials: string;

  gap: { text: string; mine: boolean } | null;

  badge: string;
}

@Component({
  selector: 'app-leaderboard',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './leaderboard.html',
  styleUrl: './leaderboard.scss',
})
export class Leaderboard {
  private readonly api = inject(Api);
  readonly current = inject(CurrentUser);

  readonly loading = signal(true);
  readonly entries = signal<LeaderboardEntry[]>([]);
  readonly activeToday = signal(0);
  readonly challenge = signal<ChallengeSummary | null>(null);

  readonly state = computed<'none' | 'upcoming' | 'live' | 'final' | 'complete'>(() => {
    const challenge = this.challenge();

    if (!challenge) {
      return 'none';
    }

    if (challenge.hasEnded) {
      return 'complete';
    }

    return challenge.day === 0 ? 'upcoming' : challenge.daysLeft === 0 ? 'final' : 'live';
  });

  readonly statusLabel = computed(() =>
    ({ none: '', upcoming: 'Upcoming', live: 'Live', final: 'Final day', complete: 'Complete' })[this.state()]);

  readonly percent = computed(() => {
    const challenge = this.challenge();

    return challenge && challenge.totalDays > 0
      ? Math.round((challenge.day / challenge.totalDays) * 100)
      : 0;
  });

  readonly rows = computed<Row[]>(() => {
    const entries = this.entries();
    const myRank = this.current.rowIn(entries)?.rank;

    return entries.map(entry => ({
      ...entry,
      initials: (entry.firstName[0] ?? '').toUpperCase() + (entry.lastName[0] ?? '').toUpperCase(),
      badge: `badge-${stableIndex(entry.userId, BadgeColours)}`,
      gap: gapFor(entry, entries, myRank),
    }));
  });

  readonly me = computed<Row | undefined>(() => this.rows().find(row => this.isMe(row)));

  readonly chase = computed(() => {
    const mine = this.me();
    const above = mine && mine.rank > 1 ? this.rows()[mine.rank - 2] : undefined;

    return mine?.pointsToOvertake && above
      ? {
          points: mine.pointsToOvertake,
          name: `${above.firstName} ${above.lastName}`,
          totalPoints: above.totalPoints,
        }
      : null;
  });

  readonly nextMove = computed(() => {
    const mine = this.me();

    if (!mine) {
      return { label: 'Get on the board', detail: 'One activity puts you on the leaderboard.' };
    }

    const target = this.chase();

    if (!target) {
      const behind = this.rows()[1];

      return {
        label: 'Protect your lead',
        detail: behind
          ? `You lead ${behind.firstName} by ${(mine.totalPoints - behind.totalPoints).toLocaleString()} pts.`
          : 'Nobody is behind you yet.',
      };
    }

    return {
      label: mine.rank === 2 ? 'Take the lead' : `Catch #${mine.rank - 1}`,
      detail: `${target.points.toLocaleString()} pts to overtake ${target.name}.`,
    };
  });

  readonly race = computed<Row[]>(() => {
    const mine = this.me();

    return mine ? this.rows().slice(Math.max(0, mine.rank - 2), mine.rank + 1) : [];
  });

  readonly change = signal<RankChange | null>(null);
  readonly explaining = signal(false);
  readonly explanation = signal<string | null>(null);
  readonly explanationFailed = signal(false);
  readonly showWhy = signal(false);

  constructor() {
    effect(() => {
      const userId = this.current.id();

      this.showWhy.set(false);
      this.explanation.set(null);
      this.explanationFailed.set(false);
      this.change.set(null);

      if (userId) {
        this.api.coachFacts(userId).subscribe({
          next: facts => this.change.set(facts.rankChange),
          error: () => this.change.set(null),
        });
      }
    });

    this.api.leaderboard().subscribe({
      next: board => {
        this.entries.set(board.entries);
        this.activeToday.set(board.activeToday);
        this.challenge.set(board.challenge);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  explain(): void {
    this.showWhy.set(!this.showWhy());

    const userId = this.current.id();

    if (!this.showWhy() || !userId || this.explanation() || this.explaining()) {
      return;
    }

    this.explaining.set(true);
    this.explanationFailed.set(false);

    this.api.insight(userId, 'rankChange').subscribe({
      next: insight => {
        this.explanation.set(insight?.reply ?? null);
        this.explaining.set(false);
      },

      error: () => {
        this.explanationFailed.set(true);
        this.explaining.set(false);
      },
    });
  }

  isMe(entry: LeaderboardEntry): boolean {
    return entry.userId === this.current.id();
  }
}

function gapFor(
  entry: LeaderboardEntry,
  entries: LeaderboardEntry[],
  myRank: number | undefined,
): { text: string; mine: boolean } | null {

  if (myRank === undefined || Math.abs(entry.rank - myRank) <= 1) {
    return null;
  }

  const mine = entries[myRank - 1].totalPoints;

  if (entry.rank > myRank) {
    return { text: `${(mine - entry.totalPoints).toLocaleString()} pts behind`, mine: false };
  }

  const cost = (entry.totalPoints - mine + 1).toLocaleString();

  return entry.rank === 1
    ? { text: `${cost} pts to #1`, mine: true }
    : { text: `${cost} pts to overtake`, mine: true };
}

function stableIndex(id: string, buckets: number): number {
  let value = 0;

  for (const character of id) {
    value = (Math.imul(value, 31) + character.charCodeAt(0)) | 0;
  }

  return Math.abs(value) % buckets;
}
