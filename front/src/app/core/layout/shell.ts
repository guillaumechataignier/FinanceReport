import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { Icon } from '../../shared/icon/icon';
import { IconName } from '../../shared/icon/icons';
import { AuthService } from '../auth/auth.service';

interface NavItem {
  path: string;
  label: string;
  icon: IconName;
  exact?: boolean;
}

/** Cadre de l'application connectée : menu latéral (FS §4.3) et zone de contenu. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  protected readonly auth = inject(AuthService);

  protected readonly items: NavItem[] = [
    { path: '/', label: 'Accueil', icon: 'home', exact: true },
    { path: '/dashboard', label: 'Tableau de bord', icon: 'dashboard' },
    { path: '/movements', label: 'Mouvements', icon: 'movements' },
    { path: '/accounts', label: 'Comptes', icon: 'accounts' },
    { path: '/securities', label: 'Supports', icon: 'securities' },
    { path: '/prices', label: 'Cours', icon: 'trend' },
    { path: '/referentials', label: 'Référentiels', icon: 'tag' },
  ];
}
