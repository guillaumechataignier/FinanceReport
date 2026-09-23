/** Formats d'affichage français (FS §4.3) : 1 234,56 €, 3,92 %, 23/09/2026. Aucun calcul financier côté front. */

const amountFormat = new Intl.NumberFormat('fr-FR', {
  style: 'currency',
  currency: 'EUR',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const percentFormat = new Intl.NumberFormat('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export function formatAmount(value: number | null | undefined, signed = false): string {
  if (value === null || value === undefined) {
    return '—';
  }

  return withSign(amountFormat.format(value), value, signed);
}

/** Prix ou cours : 2 décimales, jusqu'à 8 pour un support CRYPTO (RG-29). */
export function formatPrice(value: number | null | undefined, maxDecimals = 2): string {
  if (value === null || value === undefined) {
    return '—';
  }

  return new Intl.NumberFormat('fr-FR', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
    maximumFractionDigits: maxDecimals,
  }).format(value);
}

/** Quantité : jusqu'à 8 décimales, sans zéros inutiles. */
export function formatQuantity(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '—';
  }

  return new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 8 }).format(value);
}

/** Pourcentage déjà exprimé en % (3.92 → « 3,92 % »). */
export function formatPercent(value: number | null | undefined, signed = false): string {
  if (value === null || value === undefined) {
    return 'N/A';
  }

  return withSign(`${percentFormat.format(value)} %`, value, signed);
}

/** Date ISO (YYYY-MM-DD) au format JJ/MM/AAAA. */
export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }

  const [year, month, day] = value.split('-');
  return `${day}/${month}/${year}`;
}

/** Classe CSS d'une plus-value : verte si positive, rouge si négative (FS §4.3). */
export function gainClass(value: number | null | undefined): string {
  if (!value) {
    return '';
  }

  return value > 0 ? 'gain' : 'loss';
}

function withSign(formatted: string, value: number, signed: boolean): string {
  return signed && value > 0 ? `+${formatted}` : formatted;
}
