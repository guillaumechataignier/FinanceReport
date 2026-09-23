import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { debounceTime, forkJoin } from 'rxjs';

import { AccountsApi } from '../../core/api/accounts.api';
import { Account, Movement, MOVEMENT_TYPE_LABELS, MOVEMENT_TYPES, MovementType, priceDecimals, Security } from '../../core/api/models';
import { MovementsApi } from '../../core/api/movements.api';
import { SecuritiesApi } from '../../core/api/securities.api';
import { ConfirmService } from '../../shared/confirm-dialog/confirm-dialog';
import { Icon } from '../../shared/icon/icon';
import { Notify } from '../../shared/notify/notify';
import { PageHeader } from '../../shared/page-header/page-header';
import { AmountFrPipe, DateFrPipe, PriceFrPipe, QuantityFrPipe } from '../../shared/pipes/format.pipes';
import { toApiError } from '../../core/api/api-error';
import { describeMovementError } from './insufficient-quantity';
import { MovementDialog, MovementDialogData } from './movement-dialog';

const PAGE_SIZE = 50;

/** Écran « Mouvements » (UC-06 à UC-09) : filtres, tableau paginé, saisie et modification en modale. */
@Component({
  selector: 'app-movements-page',
  imports: [ReactiveFormsModule, PageHeader, Icon, AmountFrPipe, DateFrPipe, PriceFrPipe, QuantityFrPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './movements-page.html',
  styles: `
    .filters { display: flex; gap: 14px; align-items: flex-end; flex-wrap: wrap; padding: 18px 20px; }
    .filters .field { min-width: 150px; flex: 1; }
    .pager { display: flex; justify-content: flex-end; align-items: center; gap: 12px; padding: 12px 14px 4px; font-size: 14px; }
  `,
})
export class MovementsPage implements OnInit {
  private readonly movementsApi = inject(MovementsApi);
  private readonly accountsApi = inject(AccountsApi);
  private readonly securitiesApi = inject(SecuritiesApi);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmService);
  private readonly notify = inject(Notify);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Paramètre de requête ?new=1 : ouvre directement le formulaire (raccourci de l'accueil). */
  readonly new = input<string>();

  protected readonly types = MOVEMENT_TYPES;
  protected readonly typeLabels = MOVEMENT_TYPE_LABELS;
  protected readonly movements = signal<Movement[]>([]);
  protected readonly accounts = signal<Account[]>([]);
  protected readonly securities = signal<Security[]>([]);
  protected readonly page = signal(0);
  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.movements().length / PAGE_SIZE)));
  protected readonly pageItems = computed(() => this.movements().slice(this.page() * PAGE_SIZE, (this.page() + 1) * PAGE_SIZE));
  protected readonly range = computed(() => {
    const total = this.movements().length;
    if (total === 0) {
      return '0 sur 0';
    }

    const start = this.page() * PAGE_SIZE + 1;
    return `${start} – ${Math.min(total, start + PAGE_SIZE - 1)} sur ${total}`;
  });

  protected readonly filters = inject(NonNullableFormBuilder).group({
    accountId: [''],
    securityId: [''],
    type: ['' as MovementType | ''],
    from: [''],
    to: [''],
  });

  ngOnInit(): void {
    forkJoin({ accounts: this.accountsApi.list(true), securities: this.securitiesApi.list(true) }).subscribe({
      next: ({ accounts, securities }) => {
        this.accounts.set(accounts);
        this.securities.set(securities);
        if (this.new()) {
          this.openDialog();
          void this.router.navigate([], { queryParams: {}, replaceUrl: true });
        }
      },
      error: (error: unknown) => this.notify.error(error),
    });

    this.filters.valueChanges.pipe(debounceTime(250), takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.page.set(0);
      this.load();
    });
    this.load();
  }

  protected priceDecimalsOf(movement: Movement): number {
    const security = this.securities().find((s) => s.id === movement.securityId);
    return security ? priceDecimals(security.type) : 2;
  }

  protected openDialog(movement?: Movement): void {
    this.dialog
      .open<MovementDialog, MovementDialogData, boolean>(MovementDialog, {
        data: { movement, accounts: this.accounts(), securities: this.securities(), movements: this.movements() },
        width: '640px',
        autoFocus: 'first-tabbable',
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success(movement ? 'Mouvement modifié.' : 'Mouvement enregistré.');
          this.load();
        }
      });
  }

  protected remove(movement: Movement): void {
    this.confirm
      .confirm({
        title: 'Supprimer le mouvement',
        message: `Supprimer ce mouvement (${this.typeLabels[movement.type].toLowerCase()} du ${movement.date.split('-').reverse().join('/')}) ? Les positions et les snapshots seront recalculés.`,
        confirmLabel: 'Supprimer',
        danger: true,
      })
      .subscribe((confirmed) => {
        if (confirmed) {
          this.movementsApi.delete(movement.id).subscribe({
            next: () => {
              this.notify.success('Mouvement supprimé.');
              this.load();
            },
            error: (error: unknown) => this.notify.message(describeMovementError(toApiError(error), this.movements())),
          });
        }
      });
  }

  private load(): void {
    const { accountId, securityId, type, from, to } = this.filters.getRawValue();
    this.movementsApi.list({ accountId, securityId, type: type || null, from, to }).subscribe({
      next: (movements) => this.movements.set(movements),
      error: (error: unknown) => this.notify.error(error),
    });
  }
}
