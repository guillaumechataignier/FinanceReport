import { Pipe, PipeTransform } from '@angular/core';

import { formatAmount, formatDate, formatPercent } from './format';

@Pipe({ name: 'amountFr' })
export class AmountFrPipe implements PipeTransform {
  transform(value: number | null | undefined, signed = false): string {
    return formatAmount(value, signed);
  }
}

@Pipe({ name: 'percentFr' })
export class PercentFrPipe implements PipeTransform {
  transform(value: number | null | undefined, signed = false): string {
    return formatPercent(value, signed);
  }
}

@Pipe({ name: 'dateFr' })
export class DateFrPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return formatDate(value);
  }
}
