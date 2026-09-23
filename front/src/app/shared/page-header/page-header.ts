import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** En-tête de page : titre, sous-titre et actions projetées à droite. */
@Component({
  selector: 'app-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="page-header">
      <div>
        <h1 class="display">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="muted">{{ subtitle() }}</p>
        }
      </div>
      <div class="actions"><ng-content /></div>
    </header>
  `,
  styles: `
    .page-header {
      display: flex;
      align-items: flex-end;
      justify-content: space-between;
      gap: 24px;
    }
    h1 {
      font-size: 34px;
      font-weight: 600;
    }
    p {
      margin: 4px 0 0;
      font-size: 14px;
    }
    .actions {
      display: flex;
      align-items: center;
      gap: 12px;
    }
  `,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
