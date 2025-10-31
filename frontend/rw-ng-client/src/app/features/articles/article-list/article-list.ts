import { Component, inject, computed, signal, effect } from '@angular/core';
import { toSignal, toObservable } from '@angular/core/rxjs-interop';
import { map, startWith, catchError, of, switchMap, tap } from 'rxjs';
import { Router } from '@angular/router';
import { ArticleListItem } from '../article-list-item/article-list-item';
import { ArticlesService } from '../articles.service';
import { Article } from '../../../core/models/article.model';
import { FeedStateService } from '../../../core/services/feed-state.service';
import { AuthService } from '../../../core/services/auth.service';
import { Paginator } from '../paginator/paginator';

type ArticlesState = {
  articles: Article[];
  articlesCount: number;
  isLoading: boolean;
};

@Component({
  selector: 'app-article-list',
  imports: [ArticleListItem, Paginator],
  templateUrl: './article-list.html',
  styleUrl: './article-list.scss',
})
export class ArticleList {
  private articlesService = inject(ArticlesService);
  private feedStateService = inject(FeedStateService);
  private authService = inject(AuthService);
  private router = inject(Router);

  protected readonly ITEMS_PER_PAGE = 10;
  currentPage = signal(1);
  private localArticleUpdates = signal<Map<string, Article>>(new Map());

  private feedParams = computed(() => ({
    feedType: this.feedStateService.currentFeedType(),
    tag: this.feedStateService.currentTag(),
    page: this.currentPage(),
  }));

  constructor() {
    // Reset to page 1 when feed type or tag changes
    effect(() => {
      this.feedStateService.currentFeedType();
      this.feedStateService.currentTag();
      this.currentPage.set(1);
      // Clear local updates when feed changes
      this.localArticleUpdates.set(new Map());
    });
  }

  private articlesState = toSignal(
    toObservable(this.feedParams).pipe(
      switchMap(({ feedType, tag, page }) => {
        const offset = (page - 1) * this.ITEMS_PER_PAGE;

        const source$ =
          feedType === 'your-feed'
            ? this.articlesService.getFeed(this.ITEMS_PER_PAGE, offset)
            : this.articlesService.getArticles(
                feedType === 'tag' && tag
                  ? { tag, limit: this.ITEMS_PER_PAGE, offset }
                  : { limit: this.ITEMS_PER_PAGE, offset }
              );

        return source$.pipe(
          map((response) => ({
            articles: response.articles,
            articlesCount: response.articlesCount,
            isLoading: false,
          })),
          catchError(() => of({ articles: [], articlesCount: 0, isLoading: false }))
        );
      }),
      startWith({ articles: [], articlesCount: 0, isLoading: true } as ArticlesState)
    ),
    { initialValue: { articles: [], articlesCount: 0, isLoading: true } as ArticlesState }
  );

  articles = computed(() => {
    const baseArticles = this.articlesState().articles;
    const updates = this.localArticleUpdates();

    if (updates.size === 0) {
      return baseArticles;
    }

    return baseArticles.map((article) => updates.get(article.slug) || article);
  });

  articlesCount = computed(() => this.articlesState().articlesCount);
  isLoading = computed(() => this.articlesState().isLoading);

  onPageChange(page: number): void {
    this.currentPage.set(page);
  }

  onFavoriteToggle(article: Article): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    const operation = article.favorited
      ? this.articlesService.unfavoriteArticle(article.slug)
      : this.articlesService.favoriteArticle(article.slug);

    operation
      .pipe(
        tap((response) => {
          // Update the article in the local state using the updates map
          const currentUpdates = new Map(this.localArticleUpdates());
          currentUpdates.set(article.slug, response.article);
          this.localArticleUpdates.set(currentUpdates);
        }),
        catchError((error) => {
          console.error('Error toggling favorite:', error);
          return of(null);
        })
      )
      .subscribe();
  }
}
