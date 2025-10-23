import { Component, inject, computed } from '@angular/core';
import { toSignal, toObservable } from '@angular/core/rxjs-interop';
import { map, startWith, catchError, of, switchMap } from 'rxjs';
import { ArticleListItem } from '../article-list-item/article-list-item';
import { ArticlesService } from '../articles.service';
import { Article } from '../../../core/models/article.model';
import { FeedStateService } from '../../../core/services/feed-state.service';

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
  private feedStateService = inject(FeedStateService);

  private feedParams = computed(() => ({
    feedType: this.feedStateService.currentFeedType(),
    tag: this.feedStateService.currentTag(),
  }));

  private articlesState = toSignal(
    toObservable(this.feedParams).pipe(
      switchMap(({ feedType, tag }) => {
        const source$ =
          feedType === 'your-feed'
            ? this.articlesService.getFeed()
            : this.articlesService.getArticles(feedType === 'tag' && tag ? { tag } : undefined);

        return source$.pipe(
          map((response) => ({ articles: response.articles, isLoading: false })),
          catchError(() => of({ articles: [], isLoading: false }))
        );
      }),
      startWith({ articles: [], isLoading: true } as ArticlesState)
    ),
    { initialValue: { articles: [], isLoading: true } as ArticlesState }
  );

  articles = computed(() => this.articlesState().articles);
  isLoading = computed(() => this.articlesState().isLoading);

  onFavoriteToggle(article: Article): void {
    // Handle favorite toggle - to be implemented
    console.log('Favorite toggled for:', article.slug);
  }
}
