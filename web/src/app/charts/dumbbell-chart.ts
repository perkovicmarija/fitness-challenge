import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { SportShare, SportTotal } from '../api/models';

interface Row {
  key: string;
  label: string;
  mine: number;
  field: number;
}

@Component({
  selector: 'app-dumbbell-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (rows().length === 0) {
      <p class="empty">No activities to compare yet.</p>
    } @else {
      <div class="legend">
        <span><i class="key mine"></i>You</span>
        <span><i class="key field"></i>Everyone</span>
      </div>

      @for (row of rows(); track row.key) {
        <div class="row" tabindex="0"
             [attr.aria-label]="row.label + ': you ' + percent(row.mine) + ', everyone ' + percent(row.field)">
          <div class="name">{{ row.label }}</div>
          <div class="track">
            <div class="connector" [style.left.%]="left(row)" [style.width.%]="span(row)"></div>
            <div class="dot field" [style.left.%]="row.field * 100"></div>
            <div class="dot mine" [style.left.%]="row.mine * 100"></div>
          </div>
          <div class="value">
            <strong>{{ percent(row.mine) }}</strong>
            <span>of your points</span>
            <span class="field">Everyone: {{ percent(row.field) }}</span>
          </div>
        </div>
      }
    }
  `,
  styles: `
    .legend {
      display: flex;
      gap: 16px;
      font-size: 12px;
      color: var(--text-secondary);
      margin-bottom: 14px;
    }

    .legend span { display: flex; align-items: center; gap: 6px; }

    .key {
      width: 10px;
      height: 10px;
      border-radius: 50%;
    }

    .key.mine { background: var(--series-1); }
    .key.field { background: var(--series-2); }

    .row {
      display: grid;
      grid-template-columns: 92px 1fr 104px;
      align-items: center;
      gap: 12px;
      padding: 5px 0;
      outline: none;
      border-radius: 6px;
    }

    .row:hover, .row:focus-visible { background: var(--surface-2); }

    .name { font-size: 13px; color: var(--text-secondary); }

    .track {
      position: relative;
      height: 14px;
      border-bottom: 1px solid var(--border);
    }

    .connector {
      position: absolute;
      top: 6px;
      height: 2px;
      background: var(--border);
    }

    .dot {
      position: absolute;
      top: 2px;
      width: 10px;
      height: 10px;
      margin-left: -5px;
      border-radius: 50%;
      box-shadow: 0 0 0 2px var(--surface-1);
    }

    .dot, .connector { transition: left 450ms ease, width 450ms ease; }

    .dot.mine { background: var(--series-1); }
    .dot.field { background: var(--series-2); }

    .value {
      font-size: 13px;
      text-align: right;
      font-variant-numeric: tabular-nums;
    }

    .value strong { font-weight: 600; }
    .value span { display: block; font-size: 11px; color: var(--text-muted); }
    .value .field { color: var(--series-2); }

    @media (max-width: 520px) {
      .row { grid-template-columns: 74px 1fr 88px; gap: 8px; }
      .name, .value { font-size: 12px; }
    }
  `,
})
export class DumbbellChart {
  readonly mine = input.required<SportTotal[]>();
  readonly field = input.required<SportShare[]>();

  readonly rows = computed<Row[]>(() => {
    const fieldShares = new Map(this.field().map(entry => [key(entry.sport), entry.share]));
    const mineShares = new Map(this.mine().map(entry => [key(entry.sport), entry.share]));

    return [...new Set([...fieldShares.keys(), ...mineShares.keys()])]
      .map(sportKey => ({
        key: sportKey,
        label: label(sportKey),
        mine: mineShares.get(sportKey) ?? 0,
        field: fieldShares.get(sportKey) ?? 0,
      }))
      .sort((a, b) => b.mine - a.mine || b.field - a.field);
  });

  left(row: Row): number {
    return Math.min(row.mine, row.field) * 100;
  }

  span(row: Row): number {
    return Math.abs(row.mine - row.field) * 100;
  }

  percent(share: number): string {
    return `${Math.round(share * 100)}%`;
  }
}

const STEPS = 'steps';

function key(sport: string | null): string {
  return sport ?? STEPS;
}

function label(sportKey: string): string {
  return sportKey === STEPS ? 'Daily steps' : sportKey[0].toUpperCase() + sportKey.slice(1);
}
