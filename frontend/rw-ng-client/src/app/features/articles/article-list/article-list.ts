import { Component, inject, OnInit, signal } from '@angular/core';
import { ArticleListItem } from '../article-list-item/article-list-item';
import { ArticlesService } from '../articles.service';
import { Article } from '../../../core/models/article.model';

@Component({
  selector: 'app-article-list',
  imports: [ArticleListItem],
  templateUrl: './article-list.html',
  styleUrl: './article-list.scss',
})
export class ArticleList implements OnInit {
  private articlesService = inject(ArticlesService);

  articles = signal<Article[]>([]);
  isLoading = signal(false);

  ngOnInit(): void {
    this.loadArticles();
  }

  loadArticles(): void {
    this.isLoading.set(true);
    this.articlesService.getArticles().subscribe({
      next: (response) => {
        this.articles.set(response.articles);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  onFavoriteToggle(article: Article): void {
    // Handle favorite toggle - to be implemented
    console.log('Favorite toggled for:', article.slug);
  }
}
