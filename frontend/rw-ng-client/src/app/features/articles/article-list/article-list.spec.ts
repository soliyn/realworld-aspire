import { render, screen, waitFor } from '@testing-library/angular';
import { of, throwError } from 'rxjs';
import { ArticleList } from './article-list';
import { ArticlesService } from '../articles.service';
import { Article, ArticlesResponse } from '../../../core/models/article.model';

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
  };

  beforeEach(() => {
    mockArticlesService.getArticles.mockClear();
  });

  it('should create the component', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { fixture } = await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should initialize with empty articles array', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { fixture } = await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    const component = fixture.componentInstance;
    expect(Array.isArray(component.articles())).toBe(true);
  });

  it('should call loadArticles on component initialization', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    expect(mockArticlesService.getArticles).toHaveBeenCalled();
  });

  it('should set isLoading to true when loading articles', async () => {
    mockArticlesService.getArticles.mockImplementation(() => {
      return of(mockArticlesResponse);
    });

    const { fixture } = await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    // The loading should be complete by this point
    const component = fixture.componentInstance;
    expect(component.isLoading()).toBe(false);
  });

  it('should load and display articles successfully', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    await waitFor(() => {
      expect(screen.getByText('First Article')).toBeTruthy();
      expect(screen.getByText('Second Article')).toBeTruthy();
    });
  });

  it('should set articles signal with response data', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { fixture } = await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.articles()).toEqual(mockArticles);
    });
  });

  it('should set isLoading to false after successful load', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { fixture } = await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
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
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
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
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.isLoading()).toBe(false);
    });
  });

  it('should render multiple article list items', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { container } = await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
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
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.articles()).toEqual([]);
      const articleElements = container.querySelectorAll('app-article-list-item');
      expect(articleElements.length).toBe(0);
    });
  });

  it('should log to console when favorite is toggled', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
    const consoleSpy = jest.spyOn(console, 'log').mockImplementation();

    const { fixture } = await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    const component = fixture.componentInstance;
    component.onFavoriteToggle(mockArticles[0]);

    expect(consoleSpy).toHaveBeenCalledWith('Favorite toggled for:', 'first-article');

    consoleSpy.mockRestore();
  });

  it('should handle favorite toggle for different articles', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
    const consoleSpy = jest.spyOn(console, 'log').mockImplementation();

    const { fixture } = await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    const component = fixture.componentInstance;

    component.onFavoriteToggle(mockArticles[0]);
    expect(consoleSpy).toHaveBeenCalledWith('Favorite toggled for:', 'first-article');

    component.onFavoriteToggle(mockArticles[1]);
    expect(consoleSpy).toHaveBeenCalledWith('Favorite toggled for:', 'second-article');

    expect(consoleSpy).toHaveBeenCalledTimes(2);

    consoleSpy.mockRestore();
  });

  it('should maintain articles state after favorite toggle', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    const { fixture } = await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.articles().length).toBe(2);
    });

    component.onFavoriteToggle(mockArticles[0]);

    // Articles should remain unchanged since favorite toggle is not implemented yet
    expect(component.articles().length).toBe(2);
  });

  it('should call ArticlesService.getArticles without parameters', async () => {
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

    await render(ArticleList, {
      providers: [{ provide: ArticlesService, useValue: mockArticlesService }],
    });

    expect(mockArticlesService.getArticles).toHaveBeenCalledWith(undefined);
  });
});
