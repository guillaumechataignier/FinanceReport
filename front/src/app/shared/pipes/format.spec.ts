import { formatAmount, formatDate, formatPercent, gainClass } from './format';

/** Intl utilise des espaces insécables : on les normalise pour comparer. */
const plain = (text: string) => text.replace(/\s/g, ' ');

describe('formats français', () => {
  it('formate les montants en euros, avec signe optionnel', () => {
    expect(plain(formatAmount(6235))).toBe('6 235,00 €');
    expect(plain(formatAmount(235, true))).toBe('+235,00 €');
    expect(plain(formatAmount(-12.34, true))).toBe('-12,34 €');
    expect(plain(formatAmount(0, true))).toBe('0,00 €');
    expect(formatAmount(null)).toBe('—');
  });

  it('formate les pourcentages déjà exprimés en %', () => {
    expect(plain(formatPercent(3.92))).toBe('3,92 %');
    expect(plain(formatPercent(11.08, true))).toBe('+11,08 %');
    expect(formatPercent(null)).toBe('N/A');
  });

  it('formate les dates ISO en JJ/MM/AAAA', () => {
    expect(formatDate('2026-09-23')).toBe('23/09/2026');
    expect(formatDate(null)).toBe('—');
  });

  it('colore les plus-values', () => {
    expect(gainClass(103.2)).toBe('gain');
    expect(gainClass(-1)).toBe('loss');
    expect(gainClass(0)).toBe('');
    expect(gainClass(null)).toBe('');
  });
});
