import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { ApiError, fieldError, toApiError } from '../../core/api/api-error';
import { ReferentialItem, Security, SECURITY_TYPE_LABELS, SECURITY_TYPES, SecurityType } from '../../core/api/models';
import { ReferentialsApi } from '../../core/api/referentials.api';
import { SecuritiesApi } from '../../core/api/securities.api';
import { ConfirmService } from '../../shared/confirm-dialog/confirm-dialog';
import { Icon } from '../../shared/icon/icon';
import { Notify } from '../../shared/notify/notify';
import { PageHeader } from '../../shared/page-header/page-header';
import { SidePanel } from '../../shared/side-panel/side-panel';

type PanelState = { mode: 'create' } | { mode: 'edit'; security: Security } | null;

interface Option {
  code: string;
  label: string;
}

/** Écran « Supports » (UC-05) : référentiel des instruments, zone et secteur choisis dans les référentiels. */
@Component({
  selector: 'app-securities-page',
  imports: [ReactiveFormsModule, PageHeader, SidePanel, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './securities-page.html',
  styles: `code { font-size: 13.5px; }`,
})
export class SecuritiesPage {
  private readonly securitiesApi = inject(SecuritiesApi);
  private readonly referentialsApi = inject(ReferentialsApi);
  private readonly confirm = inject(ConfirmService);
  private readonly notify = inject(Notify);

  protected readonly types = SECURITY_TYPES;
  protected readonly typeLabels = SECURITY_TYPE_LABELS;
  protected readonly fieldError = fieldError;

  protected readonly includeArchived = signal(false);
  protected readonly securities = signal<Security[]>([]);
  protected readonly zones = signal<ReferentialItem[]>([]);
  protected readonly sectors = signal<ReferentialItem[]>([]);
  protected readonly panel = signal<PanelState>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: [''],
    code: [''],
    type: ['' as SecurityType | ''],
    zone: [''],
    sector: [''],
  });

  /** Valeurs actives, plus la valeur archivée déjà portée par le support modifié (RG-24). */
  protected readonly zoneOptions = computed(() => this.options(this.zones(), (s) => [s.zone, s.zoneLabel, s.zoneArchived]));
  protected readonly sectorOptions = computed(() => this.options(this.sectors(), (s) => [s.sector, s.sectorLabel, s.sectorArchived]));

  constructor() {
    effect(() => this.load(this.includeArchived()));
  }

  protected openCreate(): void {
    this.form.reset({ name: '', code: '', type: '', zone: '', sector: '' });
    this.error.set(null);
    this.panel.set({ mode: 'create' });
  }

  protected openEdit(security: Security): void {
    this.form.reset({ name: security.name, code: security.code, type: security.type, zone: security.zone, sector: security.sector });
    this.error.set(null);
    this.panel.set({ mode: 'edit', security });
  }

  protected save(): void {
    const panel = this.panel();
    if (!panel || this.saving()) {
      return;
    }

    const value = this.form.getRawValue();
    const request = {
      name: value.name.trim(),
      code: value.code.trim(),
      type: value.type || null,
      zone: value.zone || null,
      sector: value.sector || null,
    };
    this.saving.set(true);
    const call = panel.mode === 'edit' ? this.securitiesApi.update(panel.security.id, request) : this.securitiesApi.create(request);
    call.subscribe({
      next: () => {
        this.saving.set(false);
        this.panel.set(null);
        this.notify.success(panel.mode === 'edit' ? 'Support modifié.' : 'Support créé.');
        this.load(this.includeArchived());
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(toApiError(error));
      },
    });
  }

  protected toggleArchived(security: Security): void {
    this.securitiesApi.setArchived(security.id, !security.archived).subscribe({
      next: () => {
        this.notify.success(security.archived ? 'Support désarchivé.' : 'Support archivé : il ne peut plus recevoir de mouvement.');
        this.load(this.includeArchived());
      },
      error: (error: unknown) => this.notify.error(error),
    });
  }

  /** RG-12 : un support utilisé ne peut pas être supprimé ; l'archivage est alors proposé. */
  protected remove(security: Security): void {
    this.confirm
      .confirm({ title: 'Supprimer le support', message: `Supprimer « ${security.name} » et ses cours ?`, confirmLabel: 'Supprimer', danger: true })
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.securitiesApi.delete(security.id).subscribe({
          next: () => {
            this.notify.success('Support supprimé.');
            this.load(this.includeArchived());
          },
          error: (error: unknown) => {
            const apiError = toApiError(error);
            if (apiError.status !== 409 || security.archived) {
              this.notify.error(error);
              return;
            }

            this.confirm
              .confirm({ title: 'Support utilisé', message: `${apiError.message} Archiver « ${security.name} » ?`, confirmLabel: 'Archiver' })
              .subscribe((archive) => archive && this.toggleArchived(security));
          },
        });
      });
  }

  private options(active: ReferentialItem[], current: (s: Security) => [string, string, boolean]): Option[] {
    const options: Option[] = active.map((i) => ({ code: i.code ?? '', label: i.label }));
    const panel = this.panel();
    if (panel?.mode === 'edit') {
      const [code, label, archived] = current(panel.security);
      if (archived) {
        options.push({ code, label: `${label} (archivé)` });
      }
    }

    return options;
  }

  private load(includeArchived: boolean): void {
    forkJoin({
      securities: this.securitiesApi.list(includeArchived),
      zones: this.referentialsApi.list('zones'),
      sectors: this.referentialsApi.list('sectors'),
    }).subscribe({
      next: ({ securities, zones, sectors }) => {
        this.securities.set(securities);
        this.zones.set(zones);
        this.sectors.set(sectors);
      },
      error: (error: unknown) => this.notify.error(error),
    });
  }
}
