import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { PageHeader } from '../page-header/page-header';

/** Page provisoire d'un écran pas encore réalisé ; le titre vient des données de route. */
@Component({
  selector: 'app-placeholder-page',
  imports: [PageHeader],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="title()" />
    <section class="card muted">Écran en cours de réalisation.</section>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }
  `,
})
export class PlaceholderPage {
  readonly title = input('');
}
