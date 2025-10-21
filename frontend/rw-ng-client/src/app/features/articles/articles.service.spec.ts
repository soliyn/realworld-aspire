import { TestBed } from '@angular/core/testing';
import { HttpParams } from '@angular/common/http';
import { of } from 'rxjs';
import { ArticlesService } from './articles.service';
import { ApiService } from '../../core/services/api.service';
import { API_ENDPOINTS } from '../../core/constants/api-endpoints';
import {
  ArticleResponse,
  ArticlesResponse,
  NewArticle,
  UpdateArticle,
} from '../../core/models/article.model';
import { CommentResponse, CommentsResponse, NewComment } from '../../core/models/comment.model';

describe('ArticlesService', () => {
  let service: ArticlesService;
  let apiService: jasmine.SpyObj<ApiService>;

  const mockArticle = {
    slug: 'test-article',
    title: 'Test Article',
    description: 'Test description',
    body: 'Test body',
    tagList: ['test'],
    createdAt: '2024-01-01',
    updatedAt: '2024-01-01',
    favorited: false,
    favoritesCount: 0,
    author: {
      username: 'testuser',
      bio: '',
      image: '',
      following: false,
    },
  };

  const mockArticleResponse: ArticleResponse = {
    article: mockArticle,
  };

  const mockArticlesResponse: ArticlesResponse = {
    articles: [mockArticle],
    articlesCount: 1,
  };

  const mockComment = {
    id: 1,
    createdAt: '2024-01-01',
    updatedAt: '2024-01-01',
    body: 'Test comment',
    author: {
      username: 'testuser',
      bio: '',
      image: '',
      following: false,
    },
  };

  const mockCommentResponse: CommentResponse = {
    comment: mockComment,
  };

  const mockCommentsResponse: CommentsResponse = {
    comments: [mockComment],
  };

  beforeEach(() => {
    const apiServiceSpy = jasmine.createSpyObj('ApiService', ['get', 'post', 'put', 'delete']);

    TestBed.configureTestingModule({
      providers: [ArticlesService, { provide: ApiService, useValue: apiServiceSpy }],
    });

    service = TestBed.inject(ArticlesService);
    apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getArticles', () => {
    it('should call apiService.get with correct endpoint and no params', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getArticles().subscribe((response) => {
        expect(response).toEqual(mockArticlesResponse);
      });

      expect(apiService.get).toHaveBeenCalledWith(
        API_ENDPOINTS.articles.base,
        jasmine.any(HttpParams)
      );
    });

    it('should call apiService.get with tag parameter', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getArticles({ tag: 'angular' }).subscribe();

      const callArgs = apiService.get.calls.mostRecent().args;
      const params = callArgs[1] as HttpParams;
      expect(params.get('tag')).toBe('angular');
    });

    it('should call apiService.get with author parameter', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getArticles({ author: 'testuser' }).subscribe();

      const callArgs = apiService.get.calls.mostRecent().args;
      const params = callArgs[1] as HttpParams;
      expect(params.get('author')).toBe('testuser');
    });

    it('should call apiService.get with favorited parameter', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getArticles({ favorited: 'testuser' }).subscribe();

      const callArgs = apiService.get.calls.mostRecent().args;
      const params = callArgs[1] as HttpParams;
      expect(params.get('favorited')).toBe('testuser');
    });

    it('should call apiService.get with limit parameter', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getArticles({ limit: 10 }).subscribe();

      const callArgs = apiService.get.calls.mostRecent().args;
      const params = callArgs[1] as HttpParams;
      expect(params.get('limit')).toBe('10');
    });

    it('should call apiService.get with offset parameter', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getArticles({ offset: 20 }).subscribe();

      const callArgs = apiService.get.calls.mostRecent().args;
      const params = callArgs[1] as HttpParams;
      expect(params.get('offset')).toBe('20');
    });

    it('should call apiService.get with multiple parameters', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getArticles({ tag: 'angular', limit: 10, offset: 20 }).subscribe();

      const callArgs = apiService.get.calls.mostRecent().args;
      const params = callArgs[1] as HttpParams;
      expect(params.get('tag')).toBe('angular');
      expect(params.get('limit')).toBe('10');
      expect(params.get('offset')).toBe('20');
    });
  });

  describe('getFeed', () => {
    it('should call apiService.get with correct endpoint and no params', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getFeed().subscribe((response) => {
        expect(response).toEqual(mockArticlesResponse);
      });

      expect(apiService.get).toHaveBeenCalledWith(
        API_ENDPOINTS.articles.feed,
        jasmine.any(HttpParams)
      );
    });

    it('should call apiService.get with limit parameter', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getFeed(10).subscribe();

      const callArgs = apiService.get.calls.mostRecent().args;
      const params = callArgs[1] as HttpParams;
      expect(params.get('limit')).toBe('10');
    });

    it('should call apiService.get with offset parameter', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getFeed(undefined, 20).subscribe();

      const callArgs = apiService.get.calls.mostRecent().args;
      const params = callArgs[1] as HttpParams;
      expect(params.get('offset')).toBe('20');
    });

    it('should call apiService.get with both limit and offset parameters', () => {
      apiService.get.and.returnValue(of(mockArticlesResponse));

      service.getFeed(10, 20).subscribe();

      const callArgs = apiService.get.calls.mostRecent().args;
      const params = callArgs[1] as HttpParams;
      expect(params.get('limit')).toBe('10');
      expect(params.get('offset')).toBe('20');
    });
  });

  describe('getArticle', () => {
    it('should call apiService.get with correct endpoint', () => {
      apiService.get.and.returnValue(of(mockArticleResponse));

      service.getArticle('test-article').subscribe((response) => {
        expect(response).toEqual(mockArticleResponse);
      });

      expect(apiService.get).toHaveBeenCalledWith(API_ENDPOINTS.articles.bySlug('test-article'));
    });
  });

  describe('createArticle', () => {
    it('should call apiService.post with correct endpoint and body', () => {
      const newArticle: NewArticle = {
        title: 'New Article',
        description: 'New description',
        body: 'New body',
        tagList: ['test'],
      };

      apiService.post.and.returnValue(of(mockArticleResponse));

      service.createArticle(newArticle).subscribe((response) => {
        expect(response).toEqual(mockArticleResponse);
      });

      expect(apiService.post).toHaveBeenCalledWith(API_ENDPOINTS.articles.base, {
        article: newArticle,
      });
    });
  });

  describe('updateArticle', () => {
    it('should call apiService.put with correct endpoint and body', () => {
      const updateArticle: UpdateArticle = {
        title: 'Updated Article',
        description: 'Updated description',
        body: 'Updated body',
      };

      apiService.put.and.returnValue(of(mockArticleResponse));

      service.updateArticle('test-article', updateArticle).subscribe((response) => {
        expect(response).toEqual(mockArticleResponse);
      });

      expect(apiService.put).toHaveBeenCalledWith(API_ENDPOINTS.articles.bySlug('test-article'), {
        article: updateArticle,
      });
    });
  });

  describe('deleteArticle', () => {
    it('should call apiService.delete with correct endpoint', () => {
      apiService.delete.and.returnValue(of(undefined));

      service.deleteArticle('test-article').subscribe();

      expect(apiService.delete).toHaveBeenCalledWith(API_ENDPOINTS.articles.bySlug('test-article'));
    });
  });

  describe('favoriteArticle', () => {
    it('should call apiService.post with correct endpoint and empty body', () => {
      apiService.post.and.returnValue(of(mockArticleResponse));

      service.favoriteArticle('test-article').subscribe((response) => {
        expect(response).toEqual(mockArticleResponse);
      });

      expect(apiService.post).toHaveBeenCalledWith(
        API_ENDPOINTS.articles.favorite('test-article'),
        {}
      );
    });
  });

  describe('unfavoriteArticle', () => {
    it('should call apiService.delete with correct endpoint', () => {
      apiService.delete.and.returnValue(of(mockArticleResponse));

      service.unfavoriteArticle('test-article').subscribe((response) => {
        expect(response).toEqual(mockArticleResponse);
      });

      expect(apiService.delete).toHaveBeenCalledWith(
        API_ENDPOINTS.articles.favorite('test-article')
      );
    });
  });

  describe('getComments', () => {
    it('should call apiService.get with correct endpoint', () => {
      apiService.get.and.returnValue(of(mockCommentsResponse));

      service.getComments('test-article').subscribe((response) => {
        expect(response).toEqual(mockCommentsResponse);
      });

      expect(apiService.get).toHaveBeenCalledWith(API_ENDPOINTS.articles.comments('test-article'));
    });
  });

  describe('addComment', () => {
    it('should call apiService.post with correct endpoint and body', () => {
      const newComment: NewComment = {
        body: 'New comment',
      };

      apiService.post.and.returnValue(of(mockCommentResponse));

      service.addComment('test-article', newComment).subscribe((response) => {
        expect(response).toEqual(mockCommentResponse);
      });

      expect(apiService.post).toHaveBeenCalledWith(
        API_ENDPOINTS.articles.comments('test-article'),
        { comment: newComment }
      );
    });
  });

  describe('deleteComment', () => {
    it('should call apiService.delete with correct endpoint', () => {
      apiService.delete.and.returnValue(of(undefined));

      service.deleteComment('test-article', 1).subscribe();

      expect(apiService.delete).toHaveBeenCalledWith(
        API_ENDPOINTS.articles.commentById('test-article', 1)
      );
    });
  });
});
