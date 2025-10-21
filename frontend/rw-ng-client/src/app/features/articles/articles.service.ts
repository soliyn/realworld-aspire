import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { API_ENDPOINTS } from '../../core/constants/api-endpoints';
import {
  ArticleResponse,
  ArticlesResponse,
  NewArticle,
  UpdateArticle,
} from '../../core/models/article.model';
import { CommentResponse, CommentsResponse, NewComment } from '../../core/models/comment.model';

export interface ArticleListParams {
  tag?: string;
  author?: string;
  favorited?: string;
  limit?: number;
  offset?: number;
}

@Injectable({
  providedIn: 'root',
})
export class ArticlesService {
  private apiService = inject(ApiService);

  getArticles(params?: ArticleListParams): Observable<ArticlesResponse> {
    let httpParams = new HttpParams();

    if (params) {
      if (params.tag) httpParams = httpParams.set('tag', params.tag);
      if (params.author) httpParams = httpParams.set('author', params.author);
      if (params.favorited) httpParams = httpParams.set('favorited', params.favorited);
      if (params.limit) httpParams = httpParams.set('limit', params.limit.toString());
      if (params.offset) httpParams = httpParams.set('offset', params.offset.toString());
    }

    return this.apiService.get<ArticlesResponse>(API_ENDPOINTS.articles.base, httpParams);
  }

  getFeed(limit?: number, offset?: number): Observable<ArticlesResponse> {
    let httpParams = new HttpParams();

    if (limit) httpParams = httpParams.set('limit', limit.toString());
    if (offset) httpParams = httpParams.set('offset', offset.toString());

    return this.apiService.get<ArticlesResponse>(API_ENDPOINTS.articles.feed, httpParams);
  }

  getArticle(slug: string): Observable<ArticleResponse> {
    return this.apiService.get<ArticleResponse>(API_ENDPOINTS.articles.bySlug(slug));
  }

  createArticle(article: NewArticle): Observable<ArticleResponse> {
    return this.apiService.post<ArticleResponse>(API_ENDPOINTS.articles.base, { article });
  }

  updateArticle(slug: string, article: UpdateArticle): Observable<ArticleResponse> {
    return this.apiService.put<ArticleResponse>(API_ENDPOINTS.articles.bySlug(slug), { article });
  }

  deleteArticle(slug: string): Observable<void> {
    return this.apiService.delete<void>(API_ENDPOINTS.articles.bySlug(slug));
  }

  favoriteArticle(slug: string): Observable<ArticleResponse> {
    return this.apiService.post<ArticleResponse>(API_ENDPOINTS.articles.favorite(slug), {});
  }

  unfavoriteArticle(slug: string): Observable<ArticleResponse> {
    return this.apiService.delete<ArticleResponse>(API_ENDPOINTS.articles.favorite(slug));
  }

  getComments(slug: string): Observable<CommentsResponse> {
    return this.apiService.get<CommentsResponse>(API_ENDPOINTS.articles.comments(slug));
  }

  addComment(slug: string, comment: NewComment): Observable<CommentResponse> {
    return this.apiService.post<CommentResponse>(API_ENDPOINTS.articles.comments(slug), { comment });
  }

  deleteComment(slug: string, commentId: number): Observable<void> {
    return this.apiService.delete<void>(API_ENDPOINTS.articles.commentById(slug, commentId));
  }
}
