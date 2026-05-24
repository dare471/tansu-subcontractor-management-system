export function formatEmployees(count: number): string {
  const n = Math.abs(count) % 100;
  const n1 = n % 10;
  if (n > 10 && n < 20) return `${count} сотрудников`;
  if (n1 === 1) return `${count} сотрудник`;
  if (n1 >= 2 && n1 <= 4) return `${count} сотрудника`;
  return `${count} сотрудников`;
}
