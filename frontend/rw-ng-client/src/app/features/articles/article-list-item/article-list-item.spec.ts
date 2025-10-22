import { render, screen, fireEvent } from '@testing-library/angular';
import { ArticleListItem } from './article-list-item';
import { Article } from '../../../core/models/article.model';

describe('ArticleListItem', () => {
  const mockArticle: Article = {
    slug: 'test-article-slug',
    title: 'Test Article Title',
    description: 'This is a test article description',
    body: 'Test article body content',
    tagList: ['angular', 'testing', 'realworld'],
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
  };

  const mockFavoritedArticle: Article = {
    ...mockArticle,
    favorited: true,
    favoritesCount: 42,
  };

  it('should render the article author information', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    expect(screen.getByText('johndoe')).toBeTruthy();
    const authorImage = screen.getByAltText('johndoe') as HTMLImageElement;
    expect(authorImage.src).toBe('https://api.realworld.io/images/johndoe.jpg');
  });

  it('should render the article title and description', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    expect(screen.getByText('Test Article Title')).toBeTruthy();
    expect(screen.getByText('This is a test article description')).toBeTruthy();
  });

  it('should render the formatted creation date', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    expect(screen.getByText('January 15, 2024')).toBeTruthy();
  });

  it('should render all tags', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    expect(screen.getByText('angular')).toBeTruthy();
    expect(screen.getByText('testing')).toBeTruthy();
    expect(screen.getByText('realworld')).toBeTruthy();
  });

  it('should not render tag list when article has no tags', async () => {
    const articleWithoutTags: Article = {
      ...mockArticle,
      tagList: [],
    };

    await render(ArticleListItem, {
      inputs: {
        article: articleWithoutTags,
      },
    });

    expect(screen.queryByRole('list')).toBeNull();
  });

  it('should render favorites count', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    expect(screen.getByText('5')).toBeTruthy();
  });

  it('should render profile links with correct href', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    const profileLinks = screen.getAllByRole('link', { name: /johndoe/i });
    profileLinks.forEach((link) => {
      expect(link.getAttribute('href')).toBe('/profile/johndoe');
    });
  });

  it('should render article link with correct href', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    const articleLink = screen.getByRole('link', { name: /Test Article Title/i });
    expect(articleLink.getAttribute('href')).toBe('/article/test-article-slug');
  });

  it('should apply btn-outline-primary class when article is not favorited', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    const favoriteButton = screen.getByRole('button');
    expect(favoriteButton.classList.contains('btn-outline-primary')).toBe(true);
    expect(favoriteButton.classList.contains('btn-primary')).toBe(false);
  });

  it('should apply btn-primary class when article is favorited', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockFavoritedArticle,
      },
    });

    const favoriteButton = screen.getByRole('button');
    expect(favoriteButton.classList.contains('btn-primary')).toBe(true);
    expect(favoriteButton.classList.contains('btn-outline-primary')).toBe(false);
  });

  it('should emit favoriteToggle event when favorite button is clicked', async () => {
    const favoriteToggleSpy = jasmine.createSpy('favoriteToggle');

    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
      on: {
        favoriteToggle: favoriteToggleSpy,
      },
    });

    const favoriteButton = screen.getByRole('button');
    fireEvent.click(favoriteButton);

    expect(favoriteToggleSpy).toHaveBeenCalledWith(mockArticle);
  });

  it('should emit favoriteToggle event with correct article data', async () => {
    const favoriteToggleSpy = jasmine.createSpy('favoriteToggle');

    await render(ArticleListItem, {
      inputs: {
        article: mockFavoritedArticle,
      },
      on: {
        favoriteToggle: favoriteToggleSpy,
      },
    });

    const favoriteButton = screen.getByRole('button');
    fireEvent.click(favoriteButton);

    expect(favoriteToggleSpy).toHaveBeenCalledWith(mockFavoritedArticle);
    expect(favoriteToggleSpy).toHaveBeenCalledTimes(1);
  });

  it('should render "Read more..." text', async () => {
    await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    expect(screen.getByText('Read more...')).toBeTruthy();
  });

  it('should render heart icon in favorite button', async () => {
    const { container } = await render(ArticleListItem, {
      inputs: {
        article: mockArticle,
      },
    });

    const heartIcon = container.querySelector('i.ion-heart');
    expect(heartIcon).toBeTruthy();
  });
});
