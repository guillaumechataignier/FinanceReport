import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';

import { ApiError, fieldError, toApiError } from '../../core/api/api-error';
import {
  Account,
  isSecuritiesAccount,
  isTrade,
  Movement,
  MOVEMENT_TYPE_LABELS,
  MOVEMENT_TYPES,
  MovementRequest,
  MovementType,
  priceDecimals,
  Security,
} from '../../core/api/models';
import { MovementsApi } from '../../core/api/movements.api';
import { parseDecimal, toInputDecimal, todayIso } from '../../shared/pipes/decimal-input';
import { describeMovementError } from './insufficient-quantity';

export interface MovementDialogData {
  movement?: Movement;
  accounts: Account[];
  securities: Security[];
  /** Mouvements connus, pour nommer la vente en conflit d'un rejet 422. */
  movements?: Movement[];
}

/** Formulaire modal d'un mouvement (UC-06 à UC-08) : les champs dépendent du type choisi (FS §3.4). */
@Component({
  selector: 'app-movement-dialog',
  imports: [ReactiveFormsModule, MatDialogModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './movement-dialog.html',
  styles: `
    form { display: flex; flex-direction: column; gap: 16px; }
    .row { display: flex; gap: 12px; }
    .row > .field { flex: 1; min-width: 0; }
    .hint { font-size: 13px; color: var(--fr-muted); }
  `,
})
export class MovementDialog {
  private readonly api = inject(MovementsApi);
  private readonly ref = inject<MatDialogRef<MovementDialog, boolean>>(MatDialogRef);
  protected readonly data = inject<MovementDialogData>(MAT_DIALOG_DATA);

  protected readonly types = MOVEMENT_TYPES;
  protected readonly typeLabels = MOVEMENT_TYPE_LABELS;
  protected readonly today = todayIso();
  protected readonly editing = !!this.data.movement;
  protected readonly type = signal<MovementType>(this.data.movement?.type ?? 'ACHAT');
  protected readonly isTrade = computed(() => isTrade(this.type()));
  protected readonly saving = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly fieldError = fieldError;

  protected readonly form = inject(NonNullableFormBuilder).group({
    date: [this.data.movement?.date ?? todayIso()],
    accountId: [this.data.movement?.accountId ?? ''],
    securityId: [this.data.movement?.securityId ?? ''],
    quantity: [this.data.movement?.quantity?.toString().replace('.', ',') ?? ''],
    unitPrice: [toInputDecimal(this.data.movement?.unitPrice)],
    fees: [this.data.movement ? toInputDecimal(this.data.movement.fees) : '0,00'],
    amount: [toInputDecimal(this.data.movement?.amount)],
  });

  /** Comptes titres non archivés (RG-21, RG-27), plus le compte déjà porté par le mouvement modifié. */
  protected readonly accountOptions = this.data.accounts.filter(
    (a) => isSecuritiesAccount(a.type) && (!a.archived || a.id === this.data.movement?.accountId),
  );

  /** Supports actifs (RG-12), plus le support déjà porté par le mouvement modifié. */
  protected readonly securityOptions = this.data.securities.filter(
    (s) => !s.archived || s.id === this.data.movement?.securityId,
  );

  private readonly securityId = toSignal(this.form.controls.securityId.valueChanges, {
    initialValue: this.form.controls.securityId.value,
  });

  protected readonly precisionHint = computed(() => {
    const security = this.data.securities.find((s) => s.id === this.securityId());
    const decimals = security ? priceDecimals(security.type) : 2;
    return `Quantité : 8 décimales max. · Prix : ${decimals} décimales max. · Frais : 2 décimales max.`;
  });

  protected save(): void {
    if (this.saving()) {
      return;
    }

    const request = this.buildRequest();
    if (!request) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const movement = this.data.movement;
    const call = movement ? this.api.update(movement.id, request) : this.api.create(request);
    call.subscribe({
      next: () => this.ref.close(true),
      error: (error: unknown) => {
        this.saving.set(false);
        const apiError = toApiError(error);
        this.error.set({ ...apiError, message: describeMovementError(apiError, this.data.movements) });
      },
    });
  }

  protected cancel(): void {
    this.ref.close(false);
  }

  private buildRequest(): MovementRequest | null {
    const value = this.form.getRawValue();
    const request: MovementRequest = { type: this.type(), date: value.date, accountId: value.accountId };
    const numbers: [keyof MovementRequest, string, string][] = this.isTrade()
      ? [
          ['quantity', value.quantity, 'La quantité'],
          ['unitPrice', value.unitPrice, 'Le prix unitaire'],
          ['fees', value.fees, 'Les frais'],
        ]
      : [['amount', value.amount, 'Le montant']];

    const errors: Record<string, string[]> = {};
    for (const [field, text, label] of numbers) {
      const parsed = parseDecimal(text);
      if (Number.isNaN(parsed)) {
        errors[field] = [`${label} n'est pas un nombre valide.`];
      } else {
        (request as unknown as Record<string, unknown>)[field] = parsed;
      }
    }

    if (Object.keys(errors).length > 0) {
      this.error.set({ status: 400, error: 'VALIDATION_ERROR', message: 'Vérifiez les champs en erreur.', details: errors });
      return null;
    }

    if (this.isTrade()) {
      request.securityId = value.securityId || null;
      request.fees ??= 0;
    }

    return request;
  }
}
