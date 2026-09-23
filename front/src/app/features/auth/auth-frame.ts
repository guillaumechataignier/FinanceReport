import { ChangeDetectionStrategy, Component } from '@angular/core';

import { Icon } from '../../shared/icon/icon';

/** Cadre des écrans d'accès : panneau de marque à gauche, formulaire projeté à droite. */
@Component({
  selector: 'app-auth-frame',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="frame">
      <aside class="brand-panel">
        <div class="brand">
          <span class="logo"><app-icon name="trend" [size]="23" [strokeWidth]="2.2" /></span>
          <span class="display name">FinanceReport</span>
        </div>
        <div class="pitch">
          <p class="display tagline">Votre patrimoine, vos placements, au même endroit.</p>
          <p class="lead">Application personnelle exécutée en local. Vos données restent sur votre machine.</p>
        </div>
        <p class="version">Version MVP</p>
      </aside>
      <main class="form-area"><ng-content /></main>
    </div>
  `,
  styles: `
    .frame { display: flex; min-height: 100vh; }
    .brand-panel {
      width: min(560px, 40vw); flex-shrink: 0; background: var(--fr-sidebar); padding: 56px;
      display: flex; flex-direction: column; justify-content: space-between;
    }
    .brand { display: flex; align-items: center; gap: 12px; }
    .logo {
      width: 40px; height: 40px; border-radius: 10px; background: var(--fr-accent); color: var(--fr-sidebar);
      display: flex; align-items: center; justify-content: center;
    }
    .name { font-size: 24px; font-weight: 600; color: #fff; }
    .pitch p { margin: 0; }
    .tagline { font-size: clamp(28px, 2.8vw, 40px); line-height: 1.15; font-weight: 500; color: #fff; }
    .lead { margin-top: 16px !important; font-size: 16px; line-height: 1.55; color: var(--fr-sidebar-text); }
    .version { margin: 0; font-size: 13px; color: var(--fr-sidebar-muted); }
    .form-area { flex-grow: 1; display: flex; align-items: center; justify-content: center; padding: 32px; }
  `,
})
export class AuthFrame {}
