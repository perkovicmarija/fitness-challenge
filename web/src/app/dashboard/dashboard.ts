import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { Api } from '../api/api';
import { CurrentUser } from '../api/current-user';
import { Activity, Dashboard, LeaderboardEntry } from '../api/models';
import { ColumnChart } from '../charts/column-chart';
import { DumbbellChart } from '../charts/dumbbell-chart';
import { Heatmap } from '../charts/heatmap';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ColumnChart, DumbbellChart, Heatmap],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardPage {
  private readonly api = inject(Api);
  readonly current = inject(CurrentUser);

  readonly loading = signal(false);
  readonly data = signal<Dashboard | null>(null);
  readonly activities = signal<Activity[]>([]);
  private readonly board = signal<LeaderboardEntry[]>([]);

  readonly recap = signal<string | null>(null);
  readonly recapLoading = signal(false);
  readonly recapFailed = signal(false);

  readonly standing = computed(() => {
    const mine = this.current.rowIn(this.board());

    return mine ? { rank: mine.rank, of: this.board().length } : null;
  });

  readonly habits = computed(() => {
    const days = this.data()?.perDay ?? [];
    const active = days.filter(day => day.points > 0);

    if (active.length === 0) {
      return null;
    }

    const best = active.reduce((top, day) => (day.points > top.points ? day : top));
    const busiestWeekday = [...active
      .reduce((byDay, day) => {
        const weekday = new Date(day.date).getDay();
        return byDay.set(weekday, (byDay.get(weekday) ?? 0) + day.points);
      }, new Map<number, number>())]
      .sort((a, b) => b[1] - a[1] || a[0] - b[0])[0][0];

    return {
      activeDays: active.length,

      totalDays: WEEKS_SHOWN * 7,
      best,
      busiestWeekday: WEEKDAYS[busiestWeekday],
      averageOnActiveDays: Math.round(active.reduce((sum, day) => sum + day.points, 0) / active.length),
    };
  });

  readonly recent = computed(() => this.activities().slice(0, RECENT_COUNT));

  constructor() {
    effect(() => {
      const userId = this.current.id();

      if (!userId) {
        this.data.set(null);
        this.activities.set([]);
        return;
      }

      this.loading.set(true);
      this.api.dashboard(userId).subscribe({
        next: dashboard => {
          this.data.set(dashboard);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
      this.api.activities(userId).subscribe(activities => this.activities.set(activities));
      this.api.leaderboard().subscribe(board => this.board.set(board.entries));

      this.recap.set(null);
      this.recapFailed.set(false);
      this.recapLoading.set(true);

      this.api.insight(userId, 'weeklyRecap').subscribe({
        next: insight => {
          this.recap.set(insight?.reply ?? null);
          this.recapLoading.set(false);
        },
        error: () => {
          this.recapFailed.set(true);
          this.recapLoading.set(false);
        },
      });
    });
  }

  measurement(activity: Activity): string {
    if (activity.distance !== null) {
      return `${activity.distance} km`;
    }

    return activity.duration ?? `${activity.steps?.toLocaleString()} steps`;
  }

  readonly recentCount = RECENT_COUNT;

  sportLabel(sport: string | null): string {
    return sport === null ? 'Daily steps' : sport[0].toUpperCase() + sport.slice(1);
  }
}

const WEEKS_SHOWN = 20;

const WEEKDAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

const RECENT_COUNT = 12;
