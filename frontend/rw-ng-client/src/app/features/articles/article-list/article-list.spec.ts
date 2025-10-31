import { render, screen, waitFor } from '@testing-library/angular';
import { of, throwError } from 'rxjs';
import { signal } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { ArticleList } from './article-list';
import { ArticlesService } from '../articles.service';
import { Article, ArticlesResponse, ArticleResponse } from '../../../core/models/article.model';
import { AuthService } from '../../../core/services/auth.service';
import { FeedStateService } from '../../../core/services/feed-state.service';

describe('ArticleList', () => {
  const mockArticles: Article[] = [
    {
      slug: 'first-article',
      title: 'First Article',
      description: 'First article description',
      body: 'First article body',
      tagList: ['angular', 'testing'],
      createdAt: '2024-01-15T10:30:00.000Z',
      updatedAt: '2024-01-15T10:30:00.000Z',
      favorited: false,
      favoritesCount: 5,
      author: {
        username: 'johndoe',
        bio: 'Software developer',
        image: 'https://api.realworld.io/images/johndoe.jpg',
        following: false,
      },
    },
    {
      slug: 'second-article',
      title: 'Second Article',
      description: 'Second article description',
      body: 'Second article body',
      tagList: ['typescript', 'rxjs'],
      createdAt: '2024-01-16T14:20:00.000Z',
      updatedAt: '2024-01-16T14:20:00.000Z',
      favorited: true,
      favoritesCount: 12,
      author: {
        username: 'janedoe',
        bio: 'Frontend engineer',
        image: 'https://api.realworld.io/images/janedoe.jpg',
        following: true,
      },
    },
  ];

  const mockArticlesResponse: ArticlesResponse = {
    articles: mockArticles,
    articlesCount: 2,
  };

  const mockArticlesService = {
    getArticles: jest.fn(),
    getFeed: jest.fn(),
    favoriteArticle: jest.fn(),
    unfavoriteArticle: jest.fn(),
  };

  const mockAuthService = {
    isAuthenticated: jest.fn(),
  };

  const mockFeedStateService = {
    currentFeedType: signal('global' as 'global' | 'your-feed' | 'tag'),
    currentTag: signal(null as string | null),
  };

  beforeEach(() => {
    mockArticlesService.getArticles.mockClear();
    mockArticlesService.getFeed.mockClear();
    mockArticlesService.favoriteArticle.mockClear();
    mockArticlesService.unfavoriteArticle.mockClear();
    mockAuthService.isAuthenticated.mockClear();
  });

  const defaultProviders = [
    { provide: ArticlesService, useValue: mockArticlesService },
    { provide: AuthService, useValue: mockAuthService },
    { provide: FeedStateService, useValue: mockFeedStateService },
    provideRouter([]),
  ];

  it('should create the component', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { fixture } = await render(ArticleList, {
      providers: defaultProviders,
    });

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should initialize with empty articles array', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { fixture } = await render(ArticleList, {
      providers: defaultProviders,
    });

    const component = fixture.componentInstance;
    expect(Array.isArray(component.articles())).toBe(true);
  });

  it('should call loadArticles on component initialization', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    await render(ArticleList, {
      providers: defaultProviders,
    });

    expect(mockArticlesService.getArticles).toHaveBeenCalled();
  });

  it('should set isLoading to true when loading articles', async () => {
    mockArticlesService.getArticles.mockImplementation(() => {
      return of(mockArticlesResponse);
    });

    const { fixture } = await render(ArticleList, {
      providers: defaultProviders,
    });

    // The loading should be complete by this point
    const component = fixture.componentInstance;
    expect(component.isLoading()).toBe(false);
  });

  it('should load and display articles successfully', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    await render(ArticleList, {
      providers: defaultProviders,
    });

    await waitFor(() => {
      expect(screen.getByText('First Article')).toBeTruthy();
      expect(screen.getByText('Second Article')).toBeTruthy();
    });
  });

  it('should set articles signal with response data', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { fixture } = await render(ArticleList, {
      providers: defaultProviders,
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.articles()).toEqual(mockArticles);
    });
  });

  it('should set isLoading to false after successful load', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { fixture } = await render(ArticleList, {
      providers: defaultProviders,
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.isLoading()).toBe(false);
    });
  });

  it('should handle error when loading articles fails', async () => {
    const error = new Error('Failed to load articles');
    mockArticlesService.getArticles.mockReturnValue(throwError(() => error));

    const { fixture } = await render(ArticleList, {
      providers: defaultProviders,
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.isLoading()).toBe(false);
      expect(component.articles()).toEqual([]);
    });
  });

  it('should set isLoading to false after error', async () => {
    const error = new Error('Network error');
    mockArticlesService.getArticles.mockReturnValue(throwError(() => error));

    const { fixture } = await render(ArticleList, {
      providers: defaultProviders,
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.isLoading()).toBe(false);
    });
  });

  it('should render multiple article list items', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { container } = await render(ArticleList, {
      providers: defaultProviders,
    });

    await waitFor(() => {
      const articleElements = container.querySelectorAll('app-article-list-item');
      expect(articleElements.length).toBe(2);
    });
  });

  it('should handle empty articles response', async () => {
    const emptyResponse: ArticlesResponse = {
      articles: [],
      articlesCount: 0,
    };
    mockArticlesService.getArticles.mockReturnValue(of(emptyResponse));

    const { fixture, container } = await render(ArticleList, {
      providers: defaultProviders,
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.articles()).toEqual([]);
      const articleElements = container.querySelectorAll('app-article-list-item');
      expect(articleElements.length).toBe(0);
    });
  });

  it('should call ArticlesService.getArticles with pagination parameters', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    await render(ArticleList, {
      providers: defaultProviders,
    });

    expect(mockArticlesService.getArticles).toHaveBeenCalledWith({ limit: 10, offset: 0 });
  });

  describe('onFavoriteToggle', () => {
    it('should redirect to login when user is not authenticated', async () => {
      mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
      mockAuthService.isAuthenticated.mockReturnValue(false);

      const { fixture, debugElement } = await render(ArticleList, {
        providers: defaultProviders,
      });

      const router = debugElement.injector.get(Router);
      const navigateSpy = jest.spyOn(router, 'navigate');

      const component = fixture.componentInstance;
      component.onFavoriteToggle(mockArticles[0]);

      expect(navigateSpy).toHaveBeenCalledWith(['/login']);
      expect(mockArticlesService.favoriteArticle).not.toHaveBeenCalled();
      expect(mockArticlesService.unfavoriteArticle).not.toHaveBeenCalled();
    });

    it('should call favoriteArticle when article is not favorited', async () => {
      mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const updatedArticle = { ...mockArticles[0], favorited: true, favoritesCount: 6 };
      const articleResponse: ArticleResponse = { article: updatedArticle };
      mockArticlesService.favoriteArticle.mockReturnValue(of(articleResponse));

      const { fixture } = await render(ArticleList, {
        providers: defaultProviders,
      });

      const component = fixture.componentInstance;
      component.onFavoriteToggle(mockArticles[0]);

      expect(mockArticlesService.favoriteArticle).toHaveBeenCalledWith('first-article');
      expect(mockArticlesService.unfavoriteArticle).not.toHaveBeenCalled();
    });

    it('should call unfavoriteArticle when article is favorited', async () => {
      mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const updatedArticle = { ...mockArticles[1], favorited: false, favoritesCount: 11 };
      const articleResponse: ArticleResponse = { article: updatedArticle };
      mockArticlesService.unfavoriteArticle.mockReturnValue(of(articleResponse));

      const { fixture } = await render(ArticleList, {
        providers: defaultProviders,
      });

      const component = fixture.componentInstance;
      component.onFavoriteToggle(mockArticles[1]);

      expect(mockArticlesService.unfavoriteArticle).toHaveBeenCalledWith('second-article');
      expect(mockArticlesService.favoriteArticle).not.toHaveBeenCalled();
    });

    it('should update article state after successful favorite', async () => {
      mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const updatedArticle = { ...mockArticles[0], favorited: true, favoritesCount: 6 };
      const articleResponse: ArticleResponse = { article: updatedArticle };
      mockArticlesService.favoriteArticle.mockReturnValue(of(articleResponse));

      const { fixture } = await render(ArticleList, {
        providers: defaultProviders,
      });

      const component = fixture.componentInstance;

      await waitFor(() => {
        expect(component.articles()[0].favorited).toBe(false);
        expect(component.articles()[0].favoritesCount).toBe(5);
      });

      component.onFavoriteToggle(mockArticles[0]);

      await waitFor(() => {
        expect(component.articles()[0].favorited).toBe(true);
        expect(component.articles()[0].favoritesCount).toBe(6);
      });
    });

    it('should update article state after successful unfavorite', async () => {
      mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const updatedArticle = { ...mockArticles[1], favorited: false, favoritesCount: 11 };
      const articleResponse: ArticleResponse = { article: updatedArticle };
      mockArticlesService.unfavoriteArticle.mockReturnValue(of(articleResponse));

      const { fixture } = await render(ArticleList, {
        providers: defaultProviders,
      });

      const component = fixture.componentInstance;

      await waitFor(() => {
        expect(component.articles()[1].favorited).toBe(true);
        expect(component.articles()[1].favoritesCount).toBe(12);
      });

      component.onFavoriteToggle(mockArticles[1]);

      await waitFor(() => {
        expect(component.articles()[1].favorited).toBe(false);
        expect(component.articles()[1].favoritesCount).toBe(11);
      });
    });

    it('should only update the toggled article, not others', async () => {
      mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const updatedArticle = { ...mockArticles[0], favorited: true, favoritesCount: 6 };
      const articleResponse: ArticleResponse = { article: updatedArticle };
      mockArticlesService.favoriteArticle.mockReturnValue(of(articleResponse));

      const { fixture } = await render(ArticleList, {
        providers: defaultProviders,
      });

      const component = fixture.componentInstance;

      await waitFor(() => {
        expect(component.articles().length).toBe(2);
      });

      component.onFavoriteToggle(mockArticles[0]);

      await waitFor(() => {
        expect(component.articles()[0].favorited).toBe(true);
        // Second article should remain unchanged
        expect(component.articles()[1].favorited).toBe(true);
        expect(component.articles()[1].favoritesCount).toBe(12);
        expect(component.articles()[1].slug).toBe('second-article');
      });
    });

    it('should handle error when favorite toggle fails', async () => {
      mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const error = new Error('Failed to favorite article');
      mockArticlesService.favoriteArticle.mockReturnValue(throwError(() => error));

      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();

      const { fixture } = await render(ArticleList, {
        providers: defaultProviders,
      });

      const component = fixture.componentInstance;

      await waitFor(() => {
        expect(component.articles().length).toBe(2);
      });

      component.onFavoriteToggle(mockArticles[0]);

      await waitFor(() => {
        expect(consoleErrorSpy).toHaveBeenCalledWith('Error toggling favorite:', error);
      });

      // Article state should remain unchanged on error
      expect(component.articles()[0].favorited).toBe(false);
      expect(component.articles()[0].favoritesCount).toBe(5);

      consoleErrorSpy.mockRestore();
    });

    it('should handle multiple favorite toggles on different articles', async () => {
      mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const updatedArticle1 = { ...mockArticles[0], favorited: true, favoritesCount: 6 };
      const updatedArticle2 = { ...mockArticles[1], favorited: false, favoritesCount: 11 };
      mockArticlesService.favoriteArticle.mockReturnValue(of({ article: updatedArticle1 }));
      mockArticlesService.unfavoriteArticle.mockReturnValue(of({ article: updatedArticle2 }));

      const { fixture } = await render(ArticleList, {
        providers: defaultProviders,
      });

      const component = fixture.componentInstance;

      await waitFor(() => {
        expect(component.articles().length).toBe(2);
      });

      component.onFavoriteToggle(mockArticles[0]);
      component.onFavoriteToggle(mockArticles[1]);

      await waitFor(() => {
        expect(component.articles()[0].favorited).toBe(true);
        expect(component.articles()[0].favoritesCount).toBe(6);
        expect(component.articles()[1].favorited).toBe(false);
        expect(component.articles()[1].favoritesCount).toBe(11);
      });

      expect(mockArticlesService.favoriteArticle).toHaveBeenCalledWith('first-article');
      expect(mockArticlesService.unfavoriteArticle).toHaveBeenCalledWith('second-article');
    });

    it('should not navigate to login when user is authenticated', async () => {
      mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
      mockAuthService.isAuthenticated.mockReturnValue(true);

      const updatedArticle = { ...mockArticles[0], favorited: true, favoritesCount: 6 };
      mockArticlesService.favoriteArticle.mockReturnValue(of({ article: updatedArticle }));

      const { fixture, debugElement } = await render(ArticleList, {
        providers: defaultProviders,
      });

      const router = debugElement.injector.get(Router);
      const navigateSpy = jest.spyOn(router, 'navigate');

      const component = fixture.componentInstance;
      component.onFavoriteToggle(mockArticles[0]);

      expect(navigateSpy).not.toHaveBeenCalled();
    });
  });
});
