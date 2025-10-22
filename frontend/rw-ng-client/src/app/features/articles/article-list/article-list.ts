import { Component, inject, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, startWith, catchError, of } from 'rxjs';
import { ArticleListItem } from '../article-list-item/article-list-item';
import { ArticlesService } from '../articles.service';
import { Article } from '../../../core/models/article.model';

type ArticlesState = {
  articles: Article[];
  isLoading: boolean;
};

@Component({
  selector: 'app-article-list',
  imports: [ArticleListItem],
  templateUrl: './article-list.html',
  styleUrl: './article-list.scss',
})
export class ArticleList {
  private articlesService = inject(ArticlesService);

  private articlesState = toSignal<ArticlesState>(
    this.articlesService.getArticles().pipe(
      map((response) => ({ articles: response.articles, isLoading: false })),
      startWith({ articles: [], isLoading: true }),
      catchError(() => of({ articles: [], isLoading: false }))
    ),
    { requireSync: true }
  );

  articles = computed(() => this.articlesState().articles);
  isLoading = computed(() => this.articlesState().isLoading);

  onFavoriteToggle(article: Article): void {
    // Handle favorite toggle - to be implemented
    console.log('Favorite toggled for:', article.slug);
  }
}
