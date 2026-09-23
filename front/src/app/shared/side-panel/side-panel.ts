import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { Icon } from '../icon/icon';

/** Panneau latéral des écrans de liste (création, modification, historique). */
@Component({
  selector: 'app-side-panel',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'card side-panel', role: 'region', '[attr.aria-label]': 'title()' },
  template: `
    <div class="side-panel-header">
      <h2 class="card-title">{{ title() }}</h2>
      <button type="button" class="icon-btn" aria-label="Fermer le panneau" (click)="closed.emit()">
        <app-icon name="close" />
      </button>
    </div>
    <ng-content />
  `,
})
export class SidePanel {
  readonly title = input.required<string>();
  readonly closed = output<void>();
}
