import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { MatMenuModule } from '@angular/material/menu';

import { Icon } from '../icon/icon';

export interface FilterOption {
  value: string;
  label: string;
}

/** Filtre à choix multiple du tableau de bord : « Comptes : Tous », « Zones : 2 sélectionnées »… */
@Component({
  selector: 'app-filter-menu',
  imports: [MatMenuModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button type="button" class="filter" [class.active]="selected().length > 0" [matMenuTriggerFor]="menu">
      <span class="muted">{{ label() }} :</span>&ngsp;<span class="strong">{{ summary() }}</span>
      <app-icon name="sortDown" [size]="14" />
    </button>
    <mat-menu #menu="matMenu" class="filter-panel">
      <div class="options" (click)="$event.stopPropagation()" (keydown)="keepMenuOpen($event)" role="group" [attr.aria-label]="label()">
        @for (option of options(); track option.value) {
          <label class="option">
            <input type="checkbox" [checked]="selected().includes(option.value)" (change)="toggle(option.value)" />
            {{ option.label }}
          </label>
        } @empty {
          <span class="muted">Aucune valeur</span>
        }
        @if (selected().length > 0) {
          <button type="button" class="btn btn-ghost clear" (click)="selected.set([])">Tout effacer</button>
        }
      </div>
    </mat-menu>
  `,
  styles: `
    .filter {
      display: inline-flex; align-items: center; gap: 6px; height: 40px; padding: 0 14px; border-radius: 999px;
      border: 1px solid var(--fr-border); background: var(--fr-surface); font: 14px var(--fr-font-body); cursor: pointer;
    }
    .filter.active { border-color: var(--fr-primary); background: #eef6f3; }
    .options { display: flex; flex-direction: column; gap: 4px; padding: 8px 14px; min-width: 220px; max-height: 360px; overflow-y: auto; }
    .option { display: flex; align-items: center; gap: 10px; min-height: 36px; font-size: 14px; cursor: pointer; }
    .option input { width: 18px; height: 18px; accent-color: var(--fr-primary); }
    .clear { align-self: flex-start; height: 36px; margin-top: 4px; }
  `,
})
export class FilterMenu {
  readonly label = input.required<string>();
  readonly options = input.required<FilterOption[]>();
  readonly allLabel = input('Tous');
  readonly selected = model<string[]>([]);

  protected readonly summary = computed(() => {
    const selected = this.selected();
    if (selected.length === 0) {
      return this.allLabel();
    }

    if (selected.length === 1) {
      return this.options().find((o) => o.value === selected[0])?.label ?? selected[0];
    }

    return `${selected.length} sélectionnés`;
  });

  /** Les cases gardent le menu ouvert ; Échap et Tab le ferment comme d'habitude. */
  protected keepMenuOpen(event: KeyboardEvent): void {
    if (event.key !== 'Escape' && event.key !== 'Tab') {
      event.stopPropagation();
    }
  }

  protected toggle(value: string): void {
    this.selected.update((current) => (current.includes(value) ? current.filter((v) => v !== value) : [...current, value]));
  }
}
