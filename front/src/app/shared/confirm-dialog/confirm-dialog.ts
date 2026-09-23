import { ChangeDetectionStrategy, Component, inject, Injectable } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { map, Observable } from 'rxjs';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  danger?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [MatDialogModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>{{ data.message }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button type="button" class="btn btn-secondary" (click)="ref.close(false)">Annuler</button>
      <button type="button" class="btn" [class.btn-danger]="data.danger" [class.btn-primary]="!data.danger" (click)="ref.close(true)">
        {{ data.confirmLabel ?? 'Confirmer' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmOptions>(MAT_DIALOG_DATA);
  protected readonly ref = inject<MatDialogRef<ConfirmDialog, boolean>>(MatDialogRef);
}

/** Demande de confirmation avant une action irréversible (suppression, archivage). */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly dialog = inject(MatDialog);

  confirm(options: ConfirmOptions): Observable<boolean> {
    return this.dialog
      .open<ConfirmDialog, ConfirmOptions, boolean>(ConfirmDialog, { data: options, width: '440px', autoFocus: 'dialog' })
      .afterClosed()
      .pipe(map((result) => result === true));
  }
}
