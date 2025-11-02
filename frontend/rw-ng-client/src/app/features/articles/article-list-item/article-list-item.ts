import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Article } from '../../../core/models/article.model';

@Component({
  selector: 'app-article-list-item',
  imports: [DatePipe, RouterLink],
  templateUrl: './article-list-item.html',
  styleUrl: './article-list-item.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ArticleListItem {
  article = input.required<Article>();

  favoriteToggle = output<Article>();

  onFavoriteClick(): void {
    this.favoriteToggle.emit(this.article());
  }
}
