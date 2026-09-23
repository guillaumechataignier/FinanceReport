import { parseDecimal, toInputDecimal } from './decimal-input';

describe('saisie décimale', () => {
  it('accepte la virgule, le point et les espaces', () => {
    expect(parseDecimal('1 234,56')).toBe(1234.56);
    expect(parseDecimal('58000.12345678')).toBe(58000.12345678);
    expect(parseDecimal('-12,5')).toBe(-12.5);
    expect(parseDecimal('')).toBeNull();
    expect(parseDecimal('  ')).toBeNull();
  });

  it('rejette une saisie invalide', () => {
    expect(parseDecimal('12,5,3')).toBeNaN();
    expect(parseDecimal('abc')).toBeNaN();
    expect(parseDecimal('1e3')).toBeNaN();
  });

  it('prépare une valeur pour un champ', () => {
    expect(toInputDecimal(115)).toBe('115,00');
    expect(toInputDecimal(1.5)).toBe('1,50');
    expect(toInputDecimal(0.00012345)).toBe('0,00012345');
    expect(toInputDecimal(null)).toBe('');
  });
});
