/** Couleurs des séries : vert et or des maquettes, puis teintes de même intensité ; gris pour les liquidités. */
export const SERIES_COLORS = ['#1F6F5C', '#B8893A', '#3E6FA3', '#8E5B8A', '#5E8C3A', '#C0673E', '#2F8C8C', '#7B6D4F'];
export const CASH_COLOR = '#7A8894';
export const CASH_KEY = 'CASH';

export function colorAt(index: number, key?: string): string {
  return key === CASH_KEY ? CASH_COLOR : SERIES_COLORS[index % SERIES_COLORS.length];
}
