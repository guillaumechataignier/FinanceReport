import { ApiError } from '../../core/api/api-error';
import { Movement } from '../../core/api/models';
import { describeMovementError } from './insufficient-quantity';

const plain = (text: string) => text.replace(/\s/g, ' ');

describe('describeMovementError', () => {
  const error: ApiError = {
    status: 422,
    error: 'INSUFFICIENT_QUANTITY',
    message: 'Quantité insuffisante : 9.00000000 disponibles au 2026-09-20',
    details: { availableQuantity: 9, conflictingMovementId: 'm3' },
  };

  it('reformule la quantité et la date en français', () => {
    expect(describeMovementError(error)).toBe('Quantité insuffisante : 9 titres disponibles au 20/09/2026.');
  });

  it('nomme la vente devenue excédentaire (UC-08)', () => {
    const m3 = { id: 'm3', type: 'VENTE', date: '2026-03-10', quantity: 6 } as Movement;

    expect(plain(describeMovementError({ ...error, details: { availableQuantity: 5, conflictingMovementId: 'm3' } }, [m3]))).toBe(
      'Opération refusée : la vente du 10/03/2026 (6 titres) deviendrait excédentaire, 5 titres disponibles à cette date.',
    );
  });

  it('laisse les autres erreurs inchangées', () => {
    expect(describeMovementError({ status: 400, error: 'VALIDATION_ERROR', message: 'Date future.' })).toBe('Date future.');
  });
});
