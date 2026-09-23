import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AccountsApi } from '../../core/api/accounts.api';
import { ApiError, fieldError, toApiError } from '../../core/api/api-error';
import { DashboardApi } from '../../core/api/dashboard.api';
import { Account, ACCOUNT_TYPES, AccountType, Balance, CATEGORY_LABELS, ReferentialItem } from '../../core/api/models';
import { ReferentialsApi } from '../../core/api/referentials.api';
import { ConfirmService } from '../../shared/confirm-dialog/confirm-dialog';
import { Icon } from '../../shared/icon/icon';
import { Notify } from '../../shared/notify/notify';
import { PageHeader } from '../../shared/page-header/page-header';
import { AmountFrPipe, DateFrPipe } from '../../shared/pipes/format.pipes';
import { parseDecimal, todayIso } from '../../shared/pipes/decimal-input';
import { SidePanel } from '../../shared/side-panel/side-panel';

type PanelState = { mode: 'create' } | { mode: 'edit'; account: Account } | { mode: 'balances'; account: Account } | null;

/** Écran « Comptes » (UC-03, UC-04) : liste, formulaire de compte et panneau des soldes. */
@Component({
  selector: 'app-accounts-page',
  imports: [ReactiveFormsModule, RouterLink, NgTemplateOutlet, PageHeader, SidePanel, Icon, AmountFrPipe, DateFrPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './accounts-page.html',
})
export class AccountsPage {
  private readonly accountsApi = inject(AccountsApi);
  private readonly referentialsApi = inject(ReferentialsApi);
  private readonly dashboardApi = inject(DashboardApi);
  private readonly confirm = inject(ConfirmService);
  private readonly notify = inject(Notify);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly accountTypes = ACCOUNT_TYPES;
  protected readonly categoryLabels = CATEGORY_LABELS;
  protected readonly today = todayIso();
  protected readonly fieldError = fieldError;

  protected readonly includeArchived = signal(false);
  protected readonly accounts = signal<Account[]>([]);
  protected readonly netWorth = signal<number | null>(null);
  protected readonly activeCount = computed(() => this.accounts().filter((a) => !a.archived).length);
  protected readonly institutions = signal<ReferentialItem[]>([]);
  protected readonly panel = signal<PanelState>(null);
  protected readonly panelAccount = computed(() => {
    const panel = this.panel();
    return panel && panel.mode !== 'create' ? panel.account : null;
  });
  protected readonly balances = signal<Balance[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<ApiError | null>(null);

  protected readonly accountForm = this.fb.group({ name: [''], type: ['' as AccountType | ''], institutionId: [''] });
  protected readonly balanceForm = this.fb.group({ date: [todayIso()], amount: [''] });

  /** Établissements proposés : les actifs, plus l'établissement archivé déjà porté par le compte modifié (RG-24). */
  protected readonly institutionOptions = computed(() => {
    const panel = this.panel();
    const options = this.institutions().map((i) => ({ id: i.id, label: i.label }));
    if (panel?.mode === 'edit' && panel.account.institutionArchived) {
      options.push({ id: panel.account.institutionId, label: `${panel.account.institutionName} (archivé)` });
    }

    return options;
  });

  constructor() {
    effect(() => this.load(this.includeArchived()));
  }

  protected openCreate(): void {
    this.accountForm.reset({ name: '', type: '', institutionId: '' });
    this.openPanel({ mode: 'create' });
  }

  protected openEdit(account: Account): void {
    this.accountForm.reset({ name: account.name, type: account.type, institutionId: account.institutionId });
    this.openPanel({ mode: 'edit', account });
  }

  protected openBalances(account: Account): void {
    this.balanceForm.reset({ date: todayIso(), amount: '' });
    this.openPanel({ mode: 'balances', account });
    this.loadBalances(account.id);
  }

  protected saveAccount(): void {
    const panel = this.panel();
    if (!panel || panel.mode === 'balances' || this.saving()) {
      return;
    }

    const { name, type, institutionId } = this.accountForm.getRawValue();
    const request = { name: name.trim(), type: type || null, institutionId: institutionId || null };
    this.saving.set(true);
    const call = panel.mode === 'edit' ? this.accountsApi.update(panel.account.id, request) : this.accountsApi.create(request);
    call.subscribe({
      next: () => this.done(panel.mode === 'edit' ? 'Compte modifié.' : 'Compte créé.'),
      error: (error: unknown) => this.fail(error),
    });
  }

  protected saveBalance(): void {
    const panel = this.panel();
    if (panel?.mode !== 'balances' || this.saving()) {
      return;
    }

    const { date, amount } = this.balanceForm.getRawValue();
    const value = parseDecimal(amount);
    if (value === null || Number.isNaN(value)) {
      this.error.set({ status: 400, error: 'VALIDATION_ERROR', message: 'Saisissez un montant valide, par exemple 1 234,56.' });
      return;
    }

    this.saving.set(true);
    this.accountsApi.saveBalance(panel.account.id, date, value).subscribe({
      next: () => {
        this.saving.set(false);
        this.error.set(null);
        this.balanceForm.controls.amount.reset('');
        this.notify.success('Solde enregistré.');
        this.loadBalances(panel.account.id);
        this.load(this.includeArchived());
      },
      error: (error: unknown) => this.fail(error),
    });
  }

  protected deleteBalance(balance: Balance): void {
    const panel = this.panel();
    if (panel?.mode !== 'balances') {
      return;
    }

    this.confirm
      .confirm({ title: 'Supprimer le solde', message: 'Supprimer ce solde ? Les snapshots seront recalculés.', confirmLabel: 'Supprimer', danger: true })
      .subscribe((confirmed) => {
        if (confirmed) {
          this.accountsApi.deleteBalance(panel.account.id, balance.date).subscribe({
            next: () => {
              this.notify.success('Solde supprimé.');
              this.loadBalances(panel.account.id);
              this.load(this.includeArchived());
            },
            error: (error: unknown) => this.notify.error(error),
          });
        }
      });
  }

  protected toggleArchived(account: Account): void {
    const message = account.archived
      ? `Réintégrer « ${account.name} » au patrimoine ?`
      : `Archiver « ${account.name} » ? Il sera exclu du patrimoine et des listes de saisie ; ses données sont conservées.`;
    this.confirm
      .confirm({ title: account.archived ? 'Désarchiver le compte' : 'Archiver le compte', message, confirmLabel: account.archived ? 'Désarchiver' : 'Archiver' })
      .subscribe((confirmed) => {
        if (confirmed) {
          this.accountsApi.setArchived(account.id, !account.archived).subscribe({
            next: () => this.done(account.archived ? 'Compte désarchivé.' : 'Compte archivé.'),
            error: (error: unknown) => this.notify.error(error),
          });
        }
      });
  }

  private openPanel(state: PanelState): void {
    this.error.set(null);
    this.panel.set(state);
  }

  private done(message: string): void {
    this.saving.set(false);
    this.panel.set(null);
    this.notify.success(message);
    this.load(this.includeArchived());
  }

  private fail(error: unknown): void {
    this.saving.set(false);
    this.error.set(toApiError(error));
  }

  private load(includeArchived: boolean): void {
    this.accountsApi.list(includeArchived).subscribe({
      next: (accounts) => this.accounts.set(accounts),
      error: (error: unknown) => this.notify.error(error),
    });
    this.referentialsApi.list('institutions').subscribe((items) => this.institutions.set(items));
    this.dashboardApi.summary().subscribe((summary) => this.netWorth.set(summary.totalNetWorth));
  }

  private loadBalances(accountId: string): void {
    this.accountsApi.balances(accountId).subscribe({
      next: (balances) => this.balances.set(balances),
      error: (error: unknown) => this.notify.error(error),
    });
  }
}
