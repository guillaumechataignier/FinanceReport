import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';

import { Price, priceDecimals, Security, SECURITY_TYPE_LABELS } from '../../core/api/models';
import { SecuritiesApi } from '../../core/api/securities.api';
import { ConfirmService } from '../../shared/confirm-dialog/confirm-dialog';
import { Icon } from '../../shared/icon/icon';
import { Notify } from '../../shared/notify/notify';
import { PageHeader } from '../../shared/page-header/page-header';
import { DateFrPipe, PriceFrPipe } from '../../shared/pipes/format.pipes';
import { SidePanel } from '../../shared/side-panel/side-panel';
import { PriceQuickEntry } from './price-quick-entry';

/** Écran « Cours » (UC-10) : supports actifs, dernier cours, saisie rapide et historique par support. */
@Component({
  selector: 'app-prices-page',
  imports: [PageHeader, SidePanel, Icon, PriceQuickEntry, DateFrPipe, PriceFrPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './prices-page.html',
})
export class PricesPage implements OnInit {
  private readonly api = inject(SecuritiesApi);
  private readonly confirm = inject(ConfirmService);
  private readonly notify = inject(Notify);

  protected readonly typeLabels = SECURITY_TYPE_LABELS;
  protected readonly priceDecimals = priceDecimals;
  protected readonly securities = signal<Security[]>([]);
  protected readonly selected = signal<Security | null>(null);
  protected readonly history = signal<Price[]>([]);

  ngOnInit(): void {
    this.load();
  }

  protected openHistory(security: Security): void {
    this.selected.set(security);
    this.loadHistory(security.id);
  }

  protected onSaved(security: Security): void {
    this.notify.success(`Cours de ${security.name} enregistré.`);
    this.load();
    if (this.selected()?.id === security.id) {
      this.loadHistory(security.id);
    }
  }

  protected deletePrice(price: Price): void {
    this.confirm
      .confirm({ title: 'Supprimer le cours', message: 'Supprimer ce cours ? Les valorisations seront recalculées.', confirmLabel: 'Supprimer', danger: true })
      .subscribe((confirmed) => {
        if (confirmed) {
          this.api.deletePrice(price.securityId, price.date).subscribe({
            next: () => {
              this.notify.success('Cours supprimé.');
              this.load();
              this.loadHistory(price.securityId);
            },
            error: (error: unknown) => this.notify.error(error),
          });
        }
      });
  }

  private load(): void {
    this.api.list().subscribe({
      next: (securities) => this.securities.set(securities),
      error: (error: unknown) => this.notify.error(error),
    });
  }

  private loadHistory(securityId: string): void {
    this.api.prices(securityId).subscribe({
      next: (prices) => this.history.set(prices),
      error: (error: unknown) => this.notify.error(error),
    });
  }
}
