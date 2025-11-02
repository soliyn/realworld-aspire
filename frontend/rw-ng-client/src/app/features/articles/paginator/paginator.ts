import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-paginator',
  imports: [],
  templateUrl: './paginator.html',
  styleUrl: './paginator.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Paginator {
  totalCount = input.required<number>();
  currentPage = input<number>(1);
  itemsPerPage = input<number>(10);

  pageChange = output<number>();

  totalPages = computed(() => Math.ceil(this.totalCount() / this.itemsPerPage()));

  pages = computed(() => {
    const total = this.totalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  onPageClick(page: number, event: Event): void {
    event.preventDefault();
    if (page !== this.currentPage() && page >= 1 && page <= this.totalPages()) {
      this.pageChange.emit(page);
    }
  }
}
