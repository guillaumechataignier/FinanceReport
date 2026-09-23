/** Contrats de l'API (FS §4.2). Montants en nombres, dates au format YYYY-MM-DD. */

export type ReferentialKind = 'zones' | 'sectors' | 'institutions';
export type AccountType = 'COURANT' | 'LIVRET' | 'AUTRE' | 'CTO' | 'PEA' | 'CRYPTO';
export type AccountCategory = 'TITRES' | 'ESPECES';
export type SecurityType = 'ETF' | 'ACTION' | 'OBLIGATION' | 'CRYPTO';
export type MovementType = 'ACHAT' | 'VENTE' | 'VERSEMENT' | 'RETRAIT';

export const ACCOUNT_TYPES: AccountType[] = ['COURANT', 'LIVRET', 'AUTRE', 'CTO', 'PEA', 'CRYPTO'];
export const SECURITY_TYPES: SecurityType[] = ['ETF', 'ACTION', 'OBLIGATION', 'CRYPTO'];
export const MOVEMENT_TYPES: MovementType[] = ['ACHAT', 'VENTE', 'VERSEMENT', 'RETRAIT'];

export const SECURITY_TYPE_LABELS: Record<SecurityType, string> = {
  ETF: 'ETF',
  ACTION: 'Action',
  OBLIGATION: 'Obligation',
  CRYPTO: 'Crypto',
};

export const MOVEMENT_TYPE_LABELS: Record<MovementType, string> = {
  ACHAT: 'Achat',
  VENTE: 'Vente',
  VERSEMENT: 'Versement',
  RETRAIT: 'Retrait',
};

export const CATEGORY_LABELS: Record<AccountCategory, string> = { TITRES: 'Titres', ESPECES: 'Espèces' };

export function isSecuritiesAccount(type: AccountType): boolean {
  return type === 'CTO' || type === 'PEA' || type === 'CRYPTO';
}

export function isTrade(type: MovementType): boolean {
  return type === 'ACHAT' || type === 'VENTE';
}

/** RG-02 : décimales autorisées pour un prix ou un cours. */
export function priceDecimals(type: SecurityType): number {
  return type === 'CRYPTO' ? 8 : 2;
}

export interface ReferentialItem {
  id: string;
  code: string | null;
  label: string;
  archived: boolean;
  usageCount: number;
}

export interface ReferentialItemRequest {
  code: string | null;
  label: string;
}

export interface Account {
  id: string;
  name: string;
  type: AccountType;
  category: AccountCategory;
  institutionId: string;
  institutionName: string;
  institutionArchived: boolean;
  archived: boolean;
  currentValue: number;
  cash: number;
  cashDate: string | null;
}

export interface AccountRequest {
  name: string;
  type: AccountType | null;
  institutionId: string | null;
}

export interface Balance {
  accountId: string;
  date: string;
  amount: number;
}

export interface Security {
  id: string;
  name: string;
  code: string;
  type: SecurityType;
  zone: string;
  zoneLabel: string;
  zoneArchived: boolean;
  sector: string;
  sectorLabel: string;
  sectorArchived: boolean;
  archived: boolean;
  lastPrice: number | null;
  lastPriceDate: string | null;
}

export interface SecurityRequest {
  name: string;
  code: string;
  type: SecurityType | null;
  zone: string | null;
  sector: string | null;
}

export interface Price {
  securityId: string;
  date: string;
  price: number;
}

export interface Movement {
  id: string;
  type: MovementType;
  date: string;
  accountId: string;
  accountName: string;
  securityId: string | null;
  securityName: string | null;
  securityCode: string | null;
  quantity: number | null;
  unitPrice: number | null;
  fees: number | null;
  amount: number | null;
  total: number;
  sequence: number;
}

export interface MovementRequest {
  type: MovementType;
  date: string;
  accountId: string;
  securityId?: string | null;
  quantity?: number | null;
  unitPrice?: number | null;
  fees?: number | null;
  amount?: number | null;
}

export interface MovementFilter {
  accountId?: string | null;
  securityId?: string | null;
  type?: MovementType | null;
  from?: string | null;
  to?: string | null;
}
