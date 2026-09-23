import { ApiError } from '../../core/api/api-error';
import { Movement, MOVEMENT_TYPE_LABELS } from '../../core/api/models';
import { formatDate, formatQuantity } from '../../shared/pipes/format';

/**
 * Message lisible d'un rejet 422 (RG-10, UC-08) : quantité disponible, date et, si elle est connue,
 * la vente devenue excédentaire. Renvoie le message de l'API pour toute autre erreur.
 */
export function describeMovementError(error: ApiError, movements: Movement[] = []): string {
  if (error.error !== 'INSUFFICIENT_QUANTITY') {
    return error.message;
  }

  const available = Number(error.details?.['availableQuantity'] ?? 0);
  const date = /\d{4}-\d{2}-\d{2}/.exec(error.message)?.[0];
  const conflicting = movements.find((m) => m.id === error.details?.['conflictingMovementId']);
  const quantity = `${formatQuantity(available)} titre${available > 1 ? 's' : ''} disponible${available > 1 ? 's' : ''}`;

  if (conflicting) {
    return (
      `Opération refusée : la ${MOVEMENT_TYPE_LABELS[conflicting.type].toLowerCase()} du ${formatDate(conflicting.date)} ` +
      `(${formatQuantity(conflicting.quantity)} titres) deviendrait excédentaire, ${quantity} à cette date.`
    );
  }

  return `Quantité insuffisante : ${quantity}${date ? ` au ${formatDate(date)}` : ''}.`;
}
