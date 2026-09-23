import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { ApiError, toApiError } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { Icon } from '../../shared/icon/icon';
import { AuthFrame } from './auth-frame';

/** Écran « Création du compte » (UC-01) : unique compte d'accès, règles du mot de passe affichées (RG-16). */
@Component({
  selector: 'app-setup-page',
  imports: [ReactiveFormsModule, AuthFrame, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './setup-page.html',
  styleUrl: './auth-form.scss',
})
export class SetupPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group({
    username: [''],
    password: [''],
    passwordConfirmation: [''],
  });
  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  protected readonly rules = computed(() => {
    const { username = '', password = '', passwordConfirmation = '' } = this.value();
    return [
      { label: 'Identifiant de 3 à 50 caractères', met: username.length >= 3 && username.length <= 50 },
      { label: 'Mot de passe de 12 caractères minimum', met: password.length >= 12 },
      { label: 'Confirmation identique au mot de passe', met: password.length > 0 && password === passwordConfirmation },
    ];
  });
  protected readonly submitting = signal(false);
  protected readonly error = signal<ApiError | null>(null);

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    if (this.rules().some((rule) => !rule.met)) {
      this.error.set({ status: 400, error: 'VALIDATION_ERROR', message: 'Les règles ci-dessous doivent toutes être respectées.' });
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.auth.setup(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigate(['/login'], { state: { created: true } }),
      error: (error: unknown) => {
        this.submitting.set(false);
        const apiError = toApiError(error);
        if (apiError.status === 409) {
          void this.router.navigate(['/login']);
        } else {
          this.error.set(apiError);
        }
      },
    });
  }
}
