import { TestBed } from '@angular/core/testing';
import { ColumnChart } from './column-chart';

function chartFor(points: number[]) {
  const fixture = TestBed.createComponent(ColumnChart);
  fixture.componentRef.setInput('data', points.map((value, index) => ({
    date: `2026-09-${String(index + 1).padStart(2, '0')}`,
    points: value,
  })));
  return fixture.componentInstance;
}

describe('ColumnChart', () => {
  it('rounds the axis top to a readable number', () => {

    expect(chartFor([890]).gridLines().map(line => line.label)).toEqual(['0', '500', '1,000']);
  });

  it('scales columns against the rounded top, not against the tallest bar', () => {

    const chart = chartFor([890, 445]);

    expect(chart.height({ date: '', points: 890 })).toBe(89);
    expect(chart.height({ date: '', points: 445 })).toBe(44.5);
  });

  it('survives a period where nothing was earned', () => {
    expect(chartFor([0]).gridLines()[0].label).toBe('0');
  });
});
