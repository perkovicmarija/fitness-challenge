import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DayTotal } from '../api/models';

@Component({
  selector: 'app-column-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data().length === 0) {
      <p class="empty">Nothing logged in this period yet.</p>
    } @else {
      <div class="plot">
        @for (line of gridLines(); track line.value) {
          <div class="grid-line" [style.bottom.%]="line.percent">
            <span>{{ line.label }}</span>
          </div>
        }

        <div class="bars">
          @for (day of data(); track day.date) {
            <div class="slot" tabindex="0" [attr.aria-label]="day.date + ': ' + day.points + ' points'">
              <div class="bar" [style.height.%]="height(day)"></div>
              <div class="tip"><strong>{{ day.points }}</strong> points<br />{{ day.date }}</div>
            </div>
          }
        </div>
      </div>

      <div class="axis">
        <span>{{ data()[0].date }}</span>
        <span>{{ data()[data().length - 1].date }}</span>
      </div>
    }
  `,
  styles: `
    .plot {
      position: relative;
      height: 180px;
      margin-bottom: 8px;
    }

    .grid-line {
      position: absolute;
      left: 0;
      right: 0;
      border-top: 1px solid var(--border);
    }

    .grid-line span {
      position: absolute;
      top: -8px;
      right: 0;
      font-size: 11px;
      color: var(--text-muted);
      background: var(--surface-1);
      padding-left: 6px;
    }

    .bars {
      position: absolute;
      inset: 0;
      display: flex;
      align-items: flex-end;
      gap: 2px;
    }

    .slot {
      position: relative;
      flex: 1;
      height: 100%;
      display: flex;
      align-items: flex-end;
      justify-content: center;
      outline: none;
    }

    @keyframes grow {
      from { transform: scaleY(0); }
      to { transform: scaleY(1); }
    }

    .bar {
      transform-origin: bottom;
      animation: grow 500ms cubic-bezier(0.2, 0.7, 0.3, 1) backwards;
      width: 100%;
      max-width: 24px;
      min-height: 2px;
      background: var(--series-1);
      border-radius: 4px 4px 0 0;
      transition: filter 0.12s;
    }

    .slot:hover .bar,
    .slot:focus-visible .bar { filter: brightness(1.35); }

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
      z-index: 2;
    }

    .tip strong { color: var(--text-primary); font-size: 13px; }

    .slot:hover .tip,
    .slot:focus-visible .tip { display: block; }

    .axis {
      display: flex;
      justify-content: space-between;
      font-size: 11px;
      color: var(--text-muted);
    }
  `,
})
export class ColumnChart {
  readonly data = input.required<DayTotal[]>();

  private readonly ceiling = computed(() => niceCeiling(Math.max(...this.data().map(d => d.points), 0)));

  readonly gridLines = computed(() => {
    const top = this.ceiling();
    return [0, 0.5, 1].map(fraction => ({
      value: fraction,
      percent: fraction * 100,
      label: Math.round(top * fraction).toLocaleString(),
    }));
  });

  height(day: DayTotal): number {
    return (day.points / this.ceiling()) * 100;
  }
}

function niceCeiling(max: number): number {
  if (max <= 0) {
    return 1;
  }

  const magnitude = 10 ** Math.floor(Math.log10(max));
  const steps = max / magnitude;

  return magnitude * (steps > 5 ? 10 : steps > 2 ? 5 : steps > 1 ? 2 : 1);
}
