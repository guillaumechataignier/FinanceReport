import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

import { toApiError } from '../../core/api/api-error';

/** Messages brefs après une action (succès ou erreur de l'API). */
@Injectable({ providedIn: 'root' })
export class Notify {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, undefined, { duration: 3000, panelClass: 'notify-success' });
  }

  error(error: unknown): void {
    this.message(toApiError(error).message);
  }

  /** Message d'erreur déjà rédigé. */
  message(text: string): void {
    this.snackBar.open(text, 'Fermer', { duration: 8000, panelClass: 'notify-error' });
  }
}
