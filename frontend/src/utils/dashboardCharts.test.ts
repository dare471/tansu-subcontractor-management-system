import { describe, expect, it } from 'vitest';
import { groupVisitsByDay, lastNDaysLabels, countByKey } from './dashboardCharts';

describe('dashboardCharts', () => {
  it('lastNDaysLabels returns N labels', () => {
    expect(lastNDaysLabels(7)).toHaveLength(7);
  });

  it('groupVisitsByDay counts visits per day', () => {
    const today = new Date();
    const iso = today.toISOString();
    const labels = lastNDaysLabels(3);
    const counts = groupVisitsByDay([iso, iso], 3);
    expect(counts).toHaveLength(3);
    expect(counts[2]).toBe(2);
    expect(labels).toHaveLength(3);
  });

  it('countByKey aggregates items', () => {
    const map = countByKey(
      [{ k: 'a' }, { k: 'b' }, { k: 'a' }],
      (x) => x.k
    );
    expect(map).toEqual({ a: 2, b: 1 });
  });
});
