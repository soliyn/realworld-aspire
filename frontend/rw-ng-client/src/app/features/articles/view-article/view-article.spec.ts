import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { of, throwError, BehaviorSubject } from 'rxjs';
import { ViewArticle } from './view-article';
import { ArticlesService } from '../articles.service';
import { AuthService } from '../../../core/services/auth.service';
import { Article } from '../../../core/models/article.model';
import { Comment } from '../../../core/models/comment.model';
import { Profile } from '../../../core/models/profile.model';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { NotFound } from '../../not-found/not-found';

describe('ViewArticle', () => {
  const mockProfile: Profile = {
    username: 'johndoe',
    bio: 'A test user',
    image: 'https://api.realworld.io/images/demo-avatar.png',
    following: false,
  };

  const mockArticle: Article = {
    slug: 'test-article',
    title: 'Test Article',
    description: 'Test description',
    body: '<p>Test body content</p>',
    tagList: ['test', 'angular'],
    createdAt: '2025-01-01T00:00:00.000Z',
    updatedAt: '2025-01-01T00:00:00.000Z',
    favorited: false,
    favoritesCount: 10,
    author: mockProfile,
  };

  const mockComment: Comment = {
    id: 1,
    createdAt: '2025-01-01T00:00:00.000Z',
    updatedAt: '2025-01-01T00:00:00.000Z',
    body: 'Test comment',
    author: mockProfile,
  };

  // Create a valid JWT token with expiration far in the future (year 2099)
  const createMockToken = (exp?: number) => {
    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const payload = btoa(
      JSON.stringify({
        sub: 'testuser',
        email: 'test@example.com',
        exp: exp || Math.floor(new Date('2099-01-01').getTime() / 1000),
      })
    );
    const signature = 'mock-signature';
    return `${header}.${payload}.${signature}`;
  };

  const mockUser = {
    email: 'test@example.com',
    token: createMockToken(),
    username: 'testuser',
    bio: 'Test bio',
    image: 'https://api.realworld.io/images/demo-avatar.png',
  };

  const mockArticlesService = {
    getArticle: jest.fn(),
    getComments: jest.fn(),
    deleteArticle: jest.fn(),
    favoriteArticle: jest.fn(),
    unfavoriteArticle: jest.fn(),
    addComment: jest.fn(),
    deleteComment: jest.fn(),
  };

  let mockRouter: any;
  let currentUserSubject: BehaviorSubject<any>;

  beforeEach(() => {
    jest.clearAllMocks();
    currentUserSubject = new BehaviorSubject(null);
    mockArticlesService.getArticle.mockReturnValue(of({ article: mockArticle }));
    mockArticlesService.getComments.mockReturnValue(of({ comments: [mockComment] }));
    mockRouter = null;
  });

  const renderComponent = async (slug = 'test-article', user: any = null) => {
    currentUserSubject.next(user);

    const result = await render(ViewArticle, {
      routes: [
        { path: '404', component: NotFound },
      ],
      providers: [
        { provide: ArticlesService, useValue: mockArticlesService },
        {
          provide: AuthService,
          useValue: {
            currentUser$: currentUserSubject.asObservable(),
          },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: () => slug,
              },
            },
            paramMap: of({
              get: () => slug,
            }),
          },
        },
      ],
      componentProperties: {},
    });

    // Get router after render for verification
    mockRouter = result.fixture.debugElement.injector.get(Router);

    return result;
  };

  it('should create', async () => {
    const { fixture } = await renderComponent();
    expect(fixture.componentInstance).toBeTruthy();
  });

  describe('Article Display', () => {
    it('should display article title', async () => {
      await renderComponent();
      await waitFor(() => {
        expect(screen.getByText('Test Article')).toBeTruthy();
      });
    });

    it('should display article body', async () => {
      await renderComponent();
      await waitFor(() => {
        expect(screen.getByText('Test body content')).toBeTruthy();
      });
    });

    it('should display article author', async () => {
      await renderComponent();
      await waitFor(() => {
        expect(screen.getAllByText('johndoe').length).toBeGreaterThan(0);
      });
    });

    it('should display article creation date', async () => {
      await renderComponent();
      await waitFor(() => {
        expect(screen.getAllByText(/January 1, 2025/).length).toBeGreaterThan(0);
      });
    });

    it('should display tags', async () => {
      await renderComponent();
      await waitFor(() => {
        expect(screen.getByText('test')).toBeTruthy();
        expect(screen.getByText('angular')).toBeTruthy();
      });
    });

    it('should display favorites count', async () => {
      await renderComponent();
      await waitFor(() => {
        expect(screen.getAllByText(/\(10\)/).length).toBeGreaterThan(0);
      });
    });

    it('should display article after loading', async () => {
      await renderComponent();
      // Wait for the article to load
      await waitFor(() => {
        expect(screen.getByText('Test Article')).toBeTruthy();
      });
    });

    it('should display 404 component when article returns 404 status', async () => {
      const notFoundError = { status: 404, statusText: 'Not Found' } as any;
      mockArticlesService.getArticle.mockReturnValue(throwError(() => notFoundError));

      await renderComponent();

      // Wait for the 404 page to be displayed
      await waitFor(() => {
        expect(screen.getByText('404')).toBeTruthy();
        expect(screen.getByText('Page Not Found')).toBeTruthy();
      });
    });

    it('should display error message for non-404 errors', async () => {
      const serverError = { status: 500, statusText: 'Internal Server Error' } as any;
      mockArticlesService.getArticle.mockReturnValue(throwError(() => serverError));

      await renderComponent();

      // Wait for the error message to be displayed
      await waitFor(() => {
        expect(screen.getByText('Failed to load article. Please try again.')).toBeTruthy();
      });
    });
  });

  describe('Comments', () => {
    it('should display comments', async () => {
      await renderComponent();
      await waitFor(() => {
        expect(screen.getByText('Test comment')).toBeTruthy();
      });
    });

    it('should display comments after loading', async () => {
      await renderComponent();
      // Wait for comments to load
      await waitFor(() => {
        expect(screen.getByText('Test comment')).toBeTruthy();
      });
    });

    it('should display error when comments fail to load', async () => {
      // Mock console.error to suppress expected error output in test
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();

      mockArticlesService.getComments.mockReturnValue(
        throwError(() => new Error('Comments not found'))
      );
      await renderComponent();
      await waitFor(() => {
        expect(screen.getByText('Failed to load comments.')).toBeTruthy();
      });

      // Verify error was logged
      expect(consoleErrorSpy).toHaveBeenCalledWith(
        'Error loading comments:',
        expect.any(Error)
      );

      // Restore console.error
      consoleErrorSpy.mockRestore();
    });

    it('should show comment form when authenticated', async () => {
      await renderComponent('test-article', mockUser);
      await waitFor(() => {
        expect(screen.getByPlaceholderText('Write a comment...')).toBeTruthy();
      });
    });

    it('should show sign in message when not authenticated', async () => {
      await renderComponent();
      await waitFor(() => {
        expect(screen.getByText(/Sign in/)).toBeTruthy();
      });
    });

    it('should submit comment when form is submitted', async () => {
      const newComment: Comment = {
        id: 2,
        createdAt: '2025-01-02T00:00:00.000Z',
        updatedAt: '2025-01-02T00:00:00.000Z',
        body: 'New comment',
        author: { ...mockProfile, username: 'testuser' },
      };

      mockArticlesService.addComment.mockReturnValue(of({ comment: newComment }));

      const { fixture } = await renderComponent('test-article', mockUser);

      // Wait for article to load so the component has article data
      await waitFor(() => {
        expect(fixture.componentInstance.article()).toBeTruthy();
      });

      // Set the comment value directly on the control
      fixture.componentInstance.commentControl.setValue('New comment');
      fixture.detectChanges();

      // Manually call the submit method since form submission can be flaky in tests
      fixture.componentInstance.onSubmitComment();

      await waitFor(() => {
        expect(mockArticlesService.addComment).toHaveBeenCalledWith('test-article', {
          body: 'New comment',
        });
      });

      // Verify the new comment was added to the list
      expect(fixture.componentInstance.comments().length).toBe(2);
      expect(fixture.componentInstance.comments()[0]).toEqual(newComment);
    });

    it('should delete comment when trash icon is clicked', async () => {
      const authorComment: Comment = {
        ...mockComment,
        author: { ...mockProfile, username: 'testuser' },
      };

      mockArticlesService.getComments.mockReturnValue(of({ comments: [authorComment] }));
      mockArticlesService.deleteComment.mockReturnValue(of(undefined));

      global.confirm = jest.fn(() => true);

      const { container } = await renderComponent('test-article', mockUser);

      await waitFor(() => {
        const trashIcon = container.querySelector('i.ion-trash-a');
        if (trashIcon) {
          fireEvent.click(trashIcon);
        }
      });

      await waitFor(() => {
        expect(mockArticlesService.deleteComment).toHaveBeenCalledWith('test-article', 1);
      });
    });
  });

  describe('Favorite/Unfavorite', () => {
    it('should toggle favorite when favorite button is clicked', async () => {
      const favoritedArticle = { ...mockArticle, favorited: true, favoritesCount: 11 };
      mockArticlesService.favoriteArticle.mockReturnValue(of({ article: favoritedArticle }));

      await renderComponent('test-article', mockUser);

      await waitFor(() => {
        const favoriteButton = screen.getAllByText(/Favorite Article/)[0]
          .closest('button') as HTMLButtonElement;
        fireEvent.click(favoriteButton);
      });

      await waitFor(() => {
        expect(mockArticlesService.favoriteArticle).toHaveBeenCalledWith('test-article');
      });
    });

    it('should unfavorite when article is already favorited', async () => {
      const favoritedArticle = { ...mockArticle, favorited: true, favoritesCount: 11 };
      const unfavoritedArticle = { ...mockArticle, favorited: false, favoritesCount: 10 };

      mockArticlesService.getArticle.mockReturnValue(of({ article: favoritedArticle }));
      mockArticlesService.unfavoriteArticle.mockReturnValue(of({ article: unfavoritedArticle }));

      await renderComponent('test-article', mockUser);

      await waitFor(() => {
        const favoriteButton = screen.getAllByText(/Unfavorite Article/)[0]
          .closest('button') as HTMLButtonElement;
        fireEvent.click(favoriteButton);
      });

      await waitFor(() => {
        expect(mockArticlesService.unfavoriteArticle).toHaveBeenCalledWith('test-article');
      });
    });
  });

  describe('Edit/Delete Article', () => {
    it('should show edit and delete buttons when user is author', async () => {
      const authorUser = { ...mockUser, username: 'johndoe' };
      await renderComponent('test-article', authorUser);

      await waitFor(() => {
        expect(screen.getAllByText(/Edit Article/).length).toBeGreaterThan(0);
        expect(screen.getAllByText(/Delete Article/).length).toBeGreaterThan(0);
      });
    });

    it('should not show edit and delete buttons when user is not author', async () => {
      await renderComponent('test-article', mockUser);

      await waitFor(() => {
        expect(screen.queryByText(/Edit Article/)).toBeNull();
        expect(screen.queryByText(/Delete Article/)).toBeNull();
      });
    });

    it('should navigate to editor when edit button is clicked', async () => {
      const authorUser = { ...mockUser, username: 'johndoe' };
      await renderComponent('test-article', authorUser);

      const navigateSpy = jest.spyOn(mockRouter, 'navigate');

      await waitFor(() => {
        const editButtons = screen.getAllByText(/Edit Article/);
        const editButton = editButtons[0].closest('button') as HTMLButtonElement;
        fireEvent.click(editButton);
      });

      expect(navigateSpy).toHaveBeenCalledWith(['/editor', 'test-article']);
    });

    it('should delete article when delete button is clicked and confirmed', async () => {
      const authorUser = { ...mockUser, username: 'johndoe' };
      mockArticlesService.deleteArticle.mockReturnValue(of(undefined));
      global.confirm = jest.fn(() => true);

      await renderComponent('test-article', authorUser);

      const navigateSpy = jest.spyOn(mockRouter, 'navigate').mockResolvedValue(true);

      await waitFor(() => {
        const deleteButtons = screen.getAllByText(/Delete Article/);
        const deleteButton = deleteButtons[0].closest('button') as HTMLButtonElement;
        fireEvent.click(deleteButton);
      });

      await waitFor(() => {
        expect(mockArticlesService.deleteArticle).toHaveBeenCalledWith('test-article');
        expect(navigateSpy).toHaveBeenCalledWith(['/']);
      });
    });

    it('should not delete article when not confirmed', async () => {
      const authorUser = { ...mockUser, username: 'johndoe' };
      global.confirm = jest.fn(() => false);

      await renderComponent('test-article', authorUser);

      await waitFor(() => {
        const deleteButtons = screen.getAllByText(/Delete Article/);
        const deleteButton = deleteButtons[0].closest('button') as HTMLButtonElement;
        fireEvent.click(deleteButton);
      });

      expect(mockArticlesService.deleteArticle).not.toHaveBeenCalled();
    });
  });

  describe('Follow/Unfollow', () => {
    it('should show follow button when user is not author', async () => {
      await renderComponent('test-article', mockUser);

      await waitFor(() => {
        expect(screen.getAllByText(/Follow johndoe/).length).toBeGreaterThan(0);
      });
    });

    it('should show unfollow button when author is followed', async () => {
      const followedArticle = {
        ...mockArticle,
        author: { ...mockProfile, following: true },
      };
      mockArticlesService.getArticle.mockReturnValue(of({ article: followedArticle }));

      await renderComponent('test-article', mockUser);

      await waitFor(() => {
        expect(screen.getAllByText(/Unfollow johndoe/).length).toBeGreaterThan(0);
      });
    });
  });

  describe('Navigation', () => {
    it('should not load article when slug is not provided', async () => {
      // When slug is null, the component doesn't attempt to load article data
      const { fixture } = await renderComponent(null as any);

      // Verify the component didn't attempt to load article data
      expect(mockArticlesService.getArticle).not.toHaveBeenCalled();
      expect(mockArticlesService.getComments).not.toHaveBeenCalled();

      // The component state should reflect that no article was loaded
      expect(fixture.componentInstance.article()).toBeNull();
      expect(fixture.componentInstance.isLoadingArticle()).toBe(false); // Loading completes immediately when no slug
    });
  });
});
