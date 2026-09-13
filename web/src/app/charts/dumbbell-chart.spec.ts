import { TestBed } from '@angular/core/testing';
import { DumbbellChart } from './dumbbell-chart';

function rowsFor(mine: { sport: string | null; points: number; share: number }[],
                 field: { sport: string | null; share: number }[]) {
  const fixture = TestBed.createComponent(DumbbellChart);
  fixture.componentRef.setInput('mine', mine);
  fixture.componentRef.setInput('field', field);
  return fixture.componentInstance.rows();
}

describe('DumbbellChart', () => {
  it('shows a sport the field does even when you have never done it', () => {
    const rows = rowsFor(
      [{ sport: 'running', points: 100, share: 1 }],
      [{ sport: 'running', share: 0.5 }, { sport: 'cycling', share: 0.5 }],
    );

    const cycling = rows.find(row => row.key === 'cycling');
    expect(cycling?.mine).toBe(0);
    expect(cycling?.field).toBe(0.5);
  });

  it('labels the sportless entry as daily steps', () => {
    const rows = rowsFor([{ sport: null, points: 10, share: 1 }], [{ sport: null, share: 1 }]);

    expect(rows[0].label).toBe('Daily steps');
  });

  it('puts the sports you do most at the top', () => {
    const rows = rowsFor(
      [
        { sport: 'walking', points: 10, share: 0.2 },
        { sport: 'running', points: 40, share: 0.8 },
      ],
      [{ sport: 'running', share: 0.5 }, { sport: 'walking', share: 0.5 }],
    );

    expect(rows.map(row => row.key)).toEqual(['running', 'walking']);
  });
});
