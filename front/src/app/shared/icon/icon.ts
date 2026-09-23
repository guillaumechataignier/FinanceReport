import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { IconName, ICONS, IconShape } from './icons';

/** Icône décorative au trait ; le libellé accessible est porté par l'élément parent. */
@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { 'aria-hidden': 'true', style: 'display: inline-flex' },
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      [attr.stroke-width]="strokeWidth()"
      stroke-linecap="round"
      stroke-linejoin="round"
    >
      @for (d of shape().paths ?? []; track $index) {
        <path [attr.d]="d" />
      }
      @for (r of shape().rects ?? []; track $index) {
        <rect [attr.x]="r.x" [attr.y]="r.y" [attr.width]="r.width" [attr.height]="r.height" [attr.rx]="r.rx" />
      }
      @for (c of shape().circles ?? []; track $index) {
        <circle [attr.cx]="c.cx" [attr.cy]="c.cy" [attr.r]="c.r" />
      }
    </svg>
  `,
})
export class Icon {
  readonly name = input.required<IconName>();
  readonly size = input(18);
  readonly strokeWidth = input(1.8);

  protected readonly shape = computed<IconShape>(() => ICONS[this.name()]);
}
