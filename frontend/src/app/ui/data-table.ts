import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TableColumn } from '../core/analytics-data.service';

@Component({
  selector: 'mo-data-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <table class="tr-table" [class.tr-table--lined]="lined()">
      <thead>
        <tr>
          @for (c of columns(); track c.key) {
            <th [class.tr-table__num]="c.numeric">{{ c.label }}</th>
          }
        </tr>
      </thead>
      <tbody>
        @for (r of rows(); track r['id'] ?? $index) {
          <tr
            [style.cursor]="clickable() ? 'pointer' : null"
            (click)="clickable() && rowClick.emit(r)"
          >
            @for (c of columns(); track c.key) {
              <td [class.tr-table__num]="c.numeric">{{ r[c.key] }}</td>
            }
          </tr>
        }
      </tbody>
    </table>
  `,
})
export class DataTable {
  readonly columns = input.required<TableColumn[]>();
  readonly rows = input.required<Record<string, string>[]>();
  readonly lined = input(false);
  readonly clickable = input(false);
  readonly rowClick = output<Record<string, string>>();
}
