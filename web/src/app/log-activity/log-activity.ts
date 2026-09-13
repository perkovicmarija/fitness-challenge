import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Api } from '../api/api';
import { CurrentUser } from '../api/current-user';
import { LeaderboardEntry, Sport } from '../api/models';

@Component({
  selector: 'app-log-activity',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  templateUrl: './log-activity.html',
  styleUrl: './log-activity.scss',
})
export class LogActivity {
  private readonly api = inject(Api);
  readonly current = inject(CurrentUser);

  readonly sports = signal<Sport[]>([]);
  readonly saving = signal(false);
  readonly errors = signal<string[]>([]);
  readonly saved = signal<number | null>(null);

  private readonly before = signal<LeaderboardEntry[]>([]);

  readonly move = signal<{ from: number; to: number; passed: string | null } | null>(null);

  readonly selectedKey = signal(inject(ActivatedRoute).snapshot.queryParamMap.get('sport') ?? 'running');
  readonly when = signal(localNow());
  readonly amount = signal('');

  readonly preview = computed(() => {
    const sport = this.selected();
    const raw = this.amount().trim();

    if (!sport || raw === '') {
      return null;
    }

    if (sport.metric === 'duration') {
      const parts = /^(\d+):([0-5]\d)$/.exec(raw);

      return parts ? Math.floor(Number(parts[1]) * sport.pointsPerUnit) : null;
    }

    const quantity = Number(raw);

    return Number.isFinite(quantity) && quantity > 0
      ? Math.floor(quantity * sport.pointsPerUnit)
      : null;
  });

  readonly selected = computed(() =>
    this.sports().find(sport => (sport.sport ?? '') === this.selectedKey()));

  constructor() {
    this.api.leaderboard().subscribe(board => this.before.set(board.entries));

    this.api.sports().subscribe(sports => {
      this.sports.set(sports);

      if (!sports.some(sport => (sport.sport ?? '') === this.selectedKey())) {
        this.selectedKey.set('running');
      }
    });
  }

  submit(): void {
    const sport = this.selected();

    if (!sport) {
      return;
    }

    this.saving.set(true);
    this.errors.set([]);
    this.saved.set(null);

    const body: Record<string, unknown> = {
      userId: this.current.id(),
      datetime: withOffset(this.when()),
    };

    if (sport.sport !== null) {
      body['sport'] = sport.sport;
    }

    if (sport.metric === 'distance') {
      body['distance'] = Number(this.amount());
    } else if (sport.metric === 'duration') {
      body['duration'] = this.amount();
    } else {
      body['steps'] = Number(this.amount());
    }

    this.api.logActivity(body).subscribe({
      next: activity => {
        this.saved.set(activity.points);
        this.move.set(null);
        this.amount.set('');
        this.saving.set(false);

        this.api.leaderboard().subscribe(board => {
          this.move.set(this.movement(board.entries));
          this.before.set(board.entries);
        });
      },
      error: response => {
        const problem = response.error?.errors as Record<string, string[]> | undefined;
        this.errors.set(problem
          ? Object.values(problem).flat()
          : [response.error?.title ?? 'That activity could not be saved.']);
        this.saving.set(false);
      },
    });
  }

  private movement(after: LeaderboardEntry[]): { from: number; to: number; passed: string | null } | null {
    const from = this.current.rowIn(this.before())?.rank;
    const to = this.current.rowIn(after)?.rank;

    if (from === undefined || to === undefined || to >= from) {
      return null;
    }

    const passed = after.find(entry => entry.rank === to + 1);

    return { from, to, passed: passed ? `${passed.firstName} ${passed.lastName}` : null };
  }
}

function localNow(): string {
  const now = new Date();
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
  return now.toISOString().slice(0, 16);
}

function withOffset(localDateTime: string): string {
  const minutes = -new Date(localDateTime).getTimezoneOffset();
  const sign = minutes >= 0 ? '+' : '-';
  const pad = (value: number) => String(Math.floor(Math.abs(value))).padStart(2, '0');

  return `${localDateTime}:00${sign}${pad(minutes / 60)}:${pad(minutes % 60)}`;
}
