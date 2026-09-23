import { HttpErrorResponse } from '@angular/common/http';

/** Format d'erreur unique de l'API (FS §4.1). */
export interface ApiError {
  status: number;
  error: string;
  message: string;
  details?: Record<string, unknown>;
}

/** Convertit une erreur HTTP en {@link ApiError}, avec un message générique si le serveur est injoignable. */
export function toApiError(error: unknown): ApiError {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as Partial<ApiError> | null;
    if (body && typeof body === 'object' && typeof body.message === 'string') {
      return {
        status: error.status,
        error: body.error ?? 'UNKNOWN',
        message: body.message,
        details: body.details,
      };
    }

    if (error.status === 0) {
      return { status: 0, error: 'NETWORK', message: "Impossible de joindre l'API. Est-elle démarrée ?" };
    }

    return { status: error.status, error: 'UNKNOWN', message: `Erreur inattendue (${error.status}).` };
  }

  return { status: 0, error: 'UNKNOWN', message: 'Erreur inattendue.' };
}

/** Premier message d'erreur associé à un champ dans les détails d'une erreur de validation. */
export function fieldError(error: ApiError | null, field: string): string | null {
  const messages = error?.details?.[field];
  return Array.isArray(messages) && typeof messages[0] === 'string' ? messages[0] : null;
}
