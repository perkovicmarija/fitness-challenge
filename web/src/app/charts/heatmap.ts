import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DayTotal } from '../api/models';

const WEEKS = 20;
const DAY = 86_400_000;

interface Cell {
  date: string;
  points: number;
  level: number;
}

interface Week {
  key: string;
  month: string | null;
  days: Cell[];
}

@Component({
  selector: 'app-heatmap',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="scroll">
      <div class="cal">
        <div class="labels">
          <span>Mon</span>
          <span>Wed</span>
          <span>Fri</span>
        </div>

        <div class="weeks">
          @for (week of weeks(); track week.key) {
            <div class="week">
              <div class="month">{{ week.month }}</div>
              @for (day of week.days; track day.date) {
                <div class="cell" [class]="'lvl-' + day.level" tabindex="0"
                     [attr.aria-label]="day.date + ': ' + day.points + ' points'">
                  <span class="tip"><strong>{{ day.points }}</strong> points<br />{{ day.date }}</span>
                </div>
              }
            </div>
          }
        </div>
      </div>
    </div>

    <div class="scale">
      Quiet
      <i class="lvl-0"></i><i class="lvl-1"></i><i class="lvl-2"></i><i class="lvl-3"></i><i class="lvl-4"></i>
      Busy
    </div>
  `,
  styles: `
    .scroll { overflow-x: auto; padding-bottom: 4px; }
    .cal { display: flex; gap: 8px; justify-content: flex-start; width: 100%; min-width: 0; }

    .labels {
      display: flex;
      flex-direction: column;
      gap: 3px;
      padding-top: 18px;
      font-size: 10px;
      color: var(--text-muted);
    }

    .cal { --row: clamp(13px, calc((100% - 100px) / 21), 26px); }
    .labels span { height: calc(var(--row) * 2 + 4px); line-height: var(--row); }

    .weeks { display: flex; gap: 4px; width: 100%; max-width: 720px; min-width: 0; }
    .week { display: flex; flex-direction: column; gap: 4px; flex: 1 1 0; min-width: 0; max-width: 26px; }

    .month {
      height: 14px;
      font-size: 10px;
      color: var(--text-muted);
      white-space: nowrap;
    }

    @keyframes fade {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    .cell {
      animation: fade 400ms ease backwards;
      width: 100%;
      aspect-ratio: 1;
      border-radius: 3px;
      position: relative;
      outline: none;
    }

    .cell:hover, .cell:focus-visible { box-shadow: 0 0 0 2px var(--text-muted); }

    .lvl-0 { background: var(--heat-0); }
    .lvl-1 { background: var(--heat-1); }
    .lvl-2 { background: var(--heat-2); }
    .lvl-3 { background: var(--heat-3); }
    .lvl-4 { background: var(--heat-4); }

    .tip {
      position: absolute;
      bottom: calc(100% + 6px);
      left: 50%;
      transform: translateX(-50%);
      background: var(--surface-2);
      border: 1px solid var(--border);
      border-radius: 6px;
      padding: 6px 9px;
      font-size: 12px;
      color: var(--text-secondary);
      white-space: nowrap;
      display: none;
      pointer-events: none;
      z-index: 3;
    }

    .tip strong { color: var(--text-primary); }

    .cell:hover .tip, .cell:focus-visible .tip { display: block; }

    .scale {
      display: flex;
      align-items: center;
      gap: 4px;
      margin-top: 12px;
      font-size: 11px;
      color: var(--text-muted);
    }

    .scale i { width: 17px; height: 17px; border-radius: 3px; }
    .scale i:first-of-type { margin-left: 4px; }
    .scale i:last-of-type { margin-right: 4px; }
  `,
})
export class Heatmap {
  readonly data = input.required<DayTotal[]>();

  readonly weeks = computed<Week[]>(() => {
    const points = new Map(this.data().map(day => [day.date, day.points]));
    const busiest = Math.max(...this.data().map(day => day.points), 0);

    const now = new Date();
    const today = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate());

    const lastDay = today + ((7 - weekdayFromMonday(today)) % 7) * DAY;
    const firstDay = lastDay - (WEEKS * 7 - 1) * DAY;

    const weeks: Week[] = [];

    for (let start = firstDay; start <= lastDay; start += 7 * DAY) {
      const days = Array.from({ length: 7 }, (_, offset) => {
        const date = iso(start + offset * DAY);
        const earned = points.get(date) ?? 0;
        return { date, points: earned, level: level(earned, busiest) };
      });

      weeks.push({ key: days[0].date, month: monthLabel(start), days });
    }

    return weeks;
  });
}

function weekdayFromMonday(time: number): number {
  return (new Date(time).getUTCDay() + 6) % 7;
}

function iso(time: number): string {
  return new Date(time).toISOString().slice(0, 10);
}

function monthLabel(weekStart: number): string | null {
  for (let offset = 0; offset < 7; offset++) {
    const date = new Date(weekStart + offset * DAY);

    if (date.getUTCDate() === 1) {
      return date.toLocaleString('en', { month: 'short', timeZone: 'UTC' });
    }
  }

  return null;
}

function level(points: number, busiest: number): number {
  if (points <= 0 || busiest <= 0) {
    return 0;
  }

  return Math.min(4, Math.ceil((points / busiest) * 4));
}
