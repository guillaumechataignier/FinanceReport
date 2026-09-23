import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ApiError, fieldError, toApiError } from '../../core/api/api-error';
import { ReferentialItem, ReferentialKind } from '../../core/api/models';
import { ReferentialsApi } from '../../core/api/referentials.api';
import { ConfirmService } from '../../shared/confirm-dialog/confirm-dialog';
import { Icon } from '../../shared/icon/icon';
import { Notify } from '../../shared/notify/notify';
import { PageHeader } from '../../shared/page-header/page-header';
import { SidePanel } from '../../shared/side-panel/side-panel';

interface KindInfo {
  kind: ReferentialKind;
  tab: string;
  singular: string;
  feminine: boolean;
  hasCode: boolean;
  usage: (count: number) => string;
}

const KINDS: KindInfo[] = [
  { kind: 'zones', tab: 'Zones géographiques', singular: 'zone', feminine: true, hasCode: true, usage: (n) => plural(n, 'support') },
  { kind: 'sectors', tab: 'Secteurs', singular: 'secteur', feminine: false, hasCode: true, usage: (n) => plural(n, 'support') },
  { kind: 'institutions', tab: 'Établissements', singular: 'établissement', feminine: false, hasCode: false, usage: (n) => plural(n, 'compte') },
];

function plural(count: number, noun: string): string {
  return count === 0 ? 'Non utilisé' : `${count} ${noun}${count > 1 ? 's' : ''}`;
}

type PanelState = { mode: 'create' } | { mode: 'edit'; item: ReferentialItem } | null;

/** Écran « Référentiels » (UC-14) : onglets zones, secteurs, établissements ; panneau de création ou de modification. */
@Component({
  selector: 'app-referentials-page',
  imports: [ReactiveFormsModule, RouterLink, PageHeader, SidePanel, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './referentials-page.html',
  styles: `
    .tabs { display: flex; gap: 4px; border-bottom: 1px solid var(--fr-border); }
    .tab {
      display: inline-flex; align-items: center; gap: 8px; height: 44px; padding: 0 14px; color: var(--fr-muted);
      text-decoration: none; font-weight: 600; border-bottom: 2px solid transparent; margin-bottom: -1px;
    }
    .tab[aria-current='page'] { color: var(--fr-text); border-bottom-color: var(--fr-primary); }
    .count { font-size: 12.5px; padding: 1px 8px; border-radius: 999px; background: var(--fr-surface-muted); }
    code { font-size: 13.5px; }
  `,
})
export class ReferentialsPage {
  private readonly api = inject(ReferentialsApi);
  private readonly confirm = inject(ConfirmService);
  private readonly notify = inject(Notify);

  /** Paramètre de route :kind. */
  readonly kind = input<ReferentialKind>('zones');

  protected readonly kinds = KINDS;
  protected readonly info = computed(() => KINDS.find((k) => k.kind === this.kind()) ?? KINDS[0]);
  protected readonly includeArchived = signal(false);
  protected readonly lists = signal<Record<ReferentialKind, ReferentialItem[]>>({ zones: [], sectors: [], institutions: [] });
  protected readonly items = computed(() => this.lists()[this.info().kind]);
  protected readonly panel = signal<PanelState>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly form = inject(NonNullableFormBuilder).group({ label: [''], code: [''] });
  protected readonly fieldError = fieldError;

  /** RG-30 : le code d'une valeur utilisée n'est plus modifiable. */
  protected readonly codeLocked = computed(() => {
    const panel = this.panel();
    return panel?.mode === 'edit' && panel.item.usageCount > 0;
  });

  protected readonly panelTitle = computed(() => {
    const { feminine, singular } = this.info();
    if (this.panel()?.mode !== 'edit') {
      return `${feminine ? 'Nouvelle' : 'Nouveau'} ${singular}`;
    }

    const article = /^[aeéiou]/i.test(singular) ? "l'" : feminine ? 'la ' : 'le ';
    return `Modifier ${article}${singular}`;
  });

  constructor() {
    effect(() => {
      this.kind();
      this.panel.set(null);
    });
    effect(() => this.load(this.includeArchived()));
  }

  protected openCreate(): void {
    this.form.reset({ label: '', code: '' });
    this.form.controls.code.enable();
    this.error.set(null);
    this.panel.set({ mode: 'create' });
  }

  protected openEdit(item: ReferentialItem): void {
    this.form.reset({ label: item.label, code: item.code ?? '' });
    if (item.usageCount > 0) {
      this.form.controls.code.disable();
    } else {
      this.form.controls.code.enable();
    }

    this.error.set(null);
    this.panel.set({ mode: 'edit', item });
  }

  protected save(): void {
    const panel = this.panel();
    if (!panel || this.saving()) {
      return;
    }

    const { label, code } = this.form.getRawValue();
    const request = { label: label.trim(), code: this.info().hasCode ? code.trim() : null };
    const kind = this.info().kind;
    this.saving.set(true);
    const call = panel.mode === 'edit' ? this.api.update(kind, panel.item.id, request) : this.api.create(kind, request);
    call.subscribe({
      next: () => {
        this.saving.set(false);
        this.panel.set(null);
        this.notify.success(panel.mode === 'edit' ? 'Valeur modifiée.' : 'Valeur créée.');
        this.load(this.includeArchived());
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(toApiError(error));
      },
    });
  }

  protected toggleArchived(item: ReferentialItem): void {
    this.api.setArchived(this.info().kind, item.id, !item.archived).subscribe({
      next: () => {
        this.notify.success(item.archived ? 'Valeur désarchivée.' : 'Valeur archivée : elle n\'est plus proposée à la saisie.');
        this.load(this.includeArchived());
      },
      error: (error: unknown) => this.notify.error(error),
    });
  }

  protected remove(item: ReferentialItem): void {
    this.confirm
      .confirm({ title: 'Supprimer la valeur', message: `Supprimer « ${item.label} » ?`, confirmLabel: 'Supprimer', danger: true })
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.api.delete(this.info().kind, item.id).subscribe({
          next: () => {
            this.notify.success('Valeur supprimée.');
            this.load(this.includeArchived());
          },
          error: (error: unknown) => this.notify.error(error),
        });
      });
  }

  private load(includeArchived: boolean): void {
    forkJoin({
      zones: this.api.list('zones', includeArchived),
      sectors: this.api.list('sectors', includeArchived),
      institutions: this.api.list('institutions', includeArchived),
    }).subscribe({
      next: (lists) => this.lists.set(lists),
      error: (error: unknown) => this.notify.error(error),
    });
  }
}
