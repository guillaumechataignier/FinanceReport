import { HttpErrorResponse } from '@angular/common/http';

import { fieldError, toApiError } from './api-error';

describe('toApiError', () => {
  it("lit le format d'erreur de l'API", () => {
    const error = toApiError(
      new HttpErrorResponse({
        status: 400,
        error: { error: 'VALIDATION_ERROR', message: 'Montant invalide.', details: { amount: ['Au plus 2 décimales.'] } },
      }),
    );

    expect(error).toEqual({
      status: 400,
      error: 'VALIDATION_ERROR',
      message: 'Montant invalide.',
      details: { amount: ['Au plus 2 décimales.'] },
    });
    expect(fieldError(error, 'amount')).toBe('Au plus 2 décimales.');
    expect(fieldError(error, 'date')).toBeNull();
  });

  it("signale une API injoignable", () => {
    expect(toApiError(new HttpErrorResponse({ status: 0 })).message).toContain("Impossible de joindre l'API");
  });
});
