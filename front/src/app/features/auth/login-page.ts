import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { toApiError } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { Icon } from '../../shared/icon/icon';
import { AuthFrame } from './auth-frame';

/** Écran « Connexion » (UC-02) : message d'erreur et compte à rebours en cas de blocage (RG-18). */
@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, AuthFrame, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login-page.html',
  styleUrl: './auth-form.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private timer: ReturnType<typeof setInterval> | undefined;

  protected readonly form = inject(NonNullableFormBuilder).group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly lockSeconds = signal(0);
  protected readonly created = signal(history.state?.created === true);

  protected readonly lockMessage = computed(() => {
    const seconds = this.lockSeconds();
    if (seconds <= 0) {
      return null;
    }

    const minutes = Math.floor(seconds / 60);
    const rest = seconds % 60;
    return `Connexion bloquée après 5 échecs. Réessayez dans ${minutes} min ${rest.toString().padStart(2, '0')} s.`;
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => clearInterval(this.timer));
  }

  protected submit(): void {
    if (this.form.invalid || this.submitting() || this.lockSeconds() > 0) {
      this.form.markAllAsTouched();
      this.error.set(this.form.invalid ? 'Saisissez votre identifiant et votre mot de passe.' : this.error());
      return;
    }

    const { username, password } = this.form.getRawValue();
    this.created.set(false);
    this.submitting.set(true);
    this.error.set(null);
    this.auth.login(username, password).subscribe({
      next: () => void this.router.navigate(['/']),
      error: (error: unknown) => {
        this.submitting.set(false);
        const apiError = toApiError(error);
        if (apiError.status === 423) {
          this.startLockout(Number(apiError.details?.['retryAfterSeconds'] ?? 900));
        } else {
          this.error.set(apiError.message);
        }
      },
    });
  }

  private startLockout(seconds: number): void {
    clearInterval(this.timer);
    this.lockSeconds.set(seconds);
    this.timer = setInterval(() => {
      this.lockSeconds.update((s) => Math.max(0, s - 1));
      if (this.lockSeconds() === 0) {
        clearInterval(this.timer);
      }
    }, 1000);
  }
}
