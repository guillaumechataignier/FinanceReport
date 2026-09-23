/**
 * Saisie décimale à la française : « 1 234,56 » ou « 1234.56 » → 1234.56.
 * Renvoie null pour une saisie vide et NaN pour une saisie invalide.
 */
export function parseDecimal(text: string | null | undefined): number | null {
  const normalized = (text ?? '').replace(/[\s  ]/g, '').replace(',', '.');
  if (normalized === '') {
    return null;
  }

  return /^-?\d+(\.\d+)?$/.test(normalized) ? Number(normalized) : Number.NaN;
}

/** Valeur numérique pour un champ de saisie : 115 → « 115,00 » (au moins 2 décimales, sans séparateur de milliers). */
export function toInputDecimal(value: number | null | undefined, minDecimals = 2): string {
  if (value === null || value === undefined) {
    return '';
  }

  const [integer, decimals = ''] = value.toString().split('.');
  return `${integer},${decimals.padEnd(minDecimals, '0')}`;
}

/** Date du jour au format YYYY-MM-DD (fuseau du poste). */
export function todayIso(): string {
  const now = new Date();
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}
