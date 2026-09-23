import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';

import { toApiError } from '../../core/api/api-error';
import { Security } from '../../core/api/models';
import { SecuritiesApi } from '../../core/api/securities.api';
import { parseDecimal, todayIso } from '../../shared/pipes/decimal-input';

/** Saisie rapide d'un cours sur une ligne de l'écran « Cours » (UC-10). */
@Component({
  selector: 'app-price-quick-entry',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="quick" [formGroup]="form" (ngSubmit)="save()" novalidate>
      <label class="visually-hidden" [for]="'date-' + security().id">Date du cours de {{ security().name }}</label>
      <input [id]="'date-' + security().id" class="input" type="date" [max]="today" formControlName="date" />
      <label class="visually-hidden" [for]="'price-' + security().id">Cours de {{ security().name }}</label>
      <div class="input-suffix price">
        <input [id]="'price-' + security().id" class="input" inputmode="decimal" placeholder="0,00" formControlName="price" />
        <span>€</span>
      </div>
      <button type="submit" class="btn btn-secondary" [disabled]="saving()">Enregistrer</button>
    </form>
    @if (error()) {
      <div class="error" role="alert">{{ error() }}</div>
    }
  `,
  styles: `
    .quick { display: flex; gap: 8px; justify-content: flex-end; align-items: center; }
    .quick .input { height: 40px; font-size: 14px; }
    .quick > .input { width: 150px; }
    .price { width: 130px; }
    .btn { height: 40px; padding: 0 14px; font-size: 14px; }
    .error { margin-top: 6px; font-size: 13px; color: var(--fr-loss); text-align: right; }
  `,
})
export class PriceQuickEntry {
  private readonly api = inject(SecuritiesApi);

  readonly security = input.required<Security>();
  readonly saved = output<void>();

  protected readonly today = todayIso();
  protected readonly form = inject(NonNullableFormBuilder).group({ date: [todayIso()], price: [''] });
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected save(): void {
    const { date, price } = this.form.getRawValue();
    const value = parseDecimal(price);
    if (value === null || Number.isNaN(value)) {
      this.error.set('Saisissez un cours valide, par exemple 104,12.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.api.savePrice(this.security().id, date, value).subscribe({
      next: () => {
        this.saving.set(false);
        this.form.controls.price.reset('');
        this.saved.emit();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(toApiError(error).message);
      },
    });
  }
}
