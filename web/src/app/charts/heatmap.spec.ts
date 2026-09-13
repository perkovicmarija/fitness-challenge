import { TestBed } from '@angular/core/testing';
import { Heatmap } from './heatmap';

function weeksFor(data: { date: string; points: number }[]) {
  const fixture = TestBed.createComponent(Heatmap);
  fixture.componentRef.setInput('data', data);
  return fixture.componentInstance.weeks();
}

describe('Heatmap', () => {
  it('draws twenty whole weeks', () => {
    const weeks = weeksFor([]);

    expect(weeks).toHaveLength(20);
    expect(weeks.every(week => week.days.length === 7)).toBe(true);
  });

  it('leaves a day with no activity at the quietest level', () => {
    const cells = weeksFor([]).flatMap(week => week.days);

    expect(cells.every(cell => cell.level === 0)).toBe(true);
  });

  it('gives the busiest day the loudest level and a quiet day a lower one', () => {
    const today = new Date();
    const iso = (daysAgo: number) =>
      new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate() - daysAgo))
        .toISOString()
        .slice(0, 10);

    const cells = weeksFor([
      { date: iso(1), points: 1000 },
      { date: iso(2), points: 100 },
    ]).flatMap(week => week.days);

    expect(cells.find(cell => cell.date === iso(1))?.level).toBe(4);
    expect(cells.find(cell => cell.date === iso(2))?.level).toBe(1);
  });
});
