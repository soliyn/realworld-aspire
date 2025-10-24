import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, BehaviorSubject } from 'rxjs';
import { Profile } from './profile';
import { ProfileService } from './profile.service';
import { ArticlesService } from '../articles/articles.service';
import { AuthService, User } from '../../core/services/auth.service';
import { Profile as ProfileModel, ProfileResponse } from '../../core/models/profile.model';
import { ArticlesResponse, Article } from '../../core/models/article.model';

describe('Profile', () => {
  const mockUser: User = {
    email: 'test@example.com',
    token: 'test-token',
    username: 'testuser',
    bio: 'Test bio',
    image: 'https://example.com/image.jpg',
  };

  const mockProfile: ProfileModel = {
    username: 'otheruser',
    bio: 'Other user bio',
    image: 'https://example.com/other.jpg',
    following: false,
  };

  const mockProfileResponse: ProfileResponse = {
    profile: mockProfile,
  };

  const mockArticle: Article = {
    slug: 'test-article',
    title: 'Test Article',
    description: 'Test description',
    body: 'Test body',
    tagList: ['test'],
    createdAt: '2024-01-01',
    updatedAt: '2024-01-01',
    favorited: false,
    favoritesCount: 0,
    author: mockProfile,
  };

  const mockArticlesResponse: ArticlesResponse = {
    articles: [mockArticle],
    articlesCount: 1,
  };

  const mockProfileService = {
    getProfile: jest.fn(),
    followUser: jest.fn(),
    unfollowUser: jest.fn(),
  };

  const mockArticlesService = {
    getArticles: jest.fn(),
    getFeed: jest.fn(),
    getArticle: jest.fn(),
    createArticle: jest.fn(),
    updateArticle: jest.fn(),
    deleteArticle: jest.fn(),
    favoriteArticle: jest.fn(),
    unfavoriteArticle: jest.fn(),
    getComments: jest.fn(),
    addComment: jest.fn(),
    deleteComment: jest.fn(),
  };

  let currentUserSubject: BehaviorSubject<User | null>;
  let paramsSubject: BehaviorSubject<{ username: string }>;
  let mockRouter: any;

  beforeEach(() => {
    jest.clearAllMocks();
    currentUserSubject = new BehaviorSubject<User | null>(mockUser);
    paramsSubject = new BehaviorSubject({ username: 'otheruser' });

    mockProfileService.getProfile.mockReturnValue(of(mockProfileResponse));
    mockArticlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
  });

  const renderComponent = async (username = 'otheruser', user: User | null = mockUser) => {
    currentUserSubject.next(user);
    paramsSubject.next({ username });

    const result = await render(Profile, {
      routes: [],
      providers: [
        { provide: ProfileService, useValue: mockProfileService },
        { provide: ArticlesService, useValue: mockArticlesService },
        {
          provide: AuthService,
          useValue: {
            currentUser$: currentUserSubject.asObservable(),
            currentUserValue: user,
          },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            params: paramsSubject.asObservable(),
          },
        },
      ],
    });

    mockRouter = result.fixture.debugElement.injector.get(Router);
    jest.spyOn(mockRouter, 'navigate');

    return result;
  };

  it('should create', async () => {
    const { fixture } = await renderComponent();
    expect(fixture.componentInstance).toBeTruthy();
  });

  describe('Profile Loading', () => {
    it('should load profile on init', async () => {
      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(mockProfileService.getProfile).toHaveBeenCalledWith('otheruser');
        expect(fixture.componentInstance.profile()).toEqual(mockProfile);
        expect(fixture.componentInstance.isLoadingProfile()).toBe(false);
      });
    });

    it('should handle profile loading error', async () => {
      mockProfileService.getProfile.mockReturnValue(
        throwError(() => ({ message: 'Profile not found' }))
      );

      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeNull();
        expect(fixture.componentInstance.profileError()).toBe('Profile not found');
      });
    });

    it('should handle generic profile loading error', async () => {
      mockProfileService.getProfile.mockReturnValue(throwError(() => ({})));

      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profileError()).toBe('Failed to load profile');
      });
    });
  });

  describe('Articles Loading', () => {
    it('should load user articles by default', async () => {
      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(mockArticlesService.getArticles).toHaveBeenCalledWith({ author: 'otheruser' });
        expect(fixture.componentInstance.articles()).toEqual([mockArticle]);
        expect(fixture.componentInstance.isLoadingArticles()).toBe(false);
      });
    });

    it('should load favorited articles when tab is changed', async () => {
      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeTruthy();
      });

      mockArticlesService.getArticles.mockClear();

      const event = new Event('click');
      fixture.componentInstance.selectTab(event, 'favorited-articles');

      await waitFor(() => {
        expect(mockArticlesService.getArticles).toHaveBeenCalledWith({ favorited: 'otheruser' });
      });
    });

    it('should handle articles loading error', async () => {
      mockArticlesService.getArticles.mockReturnValue(
        throwError(() => ({ message: 'Failed' }))
      );

      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.articles()).toEqual([]);
      });
    });
  });

  describe('Tab Selection', () => {
    it('should start with my-articles tab selected', async () => {
      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.currentTab()).toBe('my-articles');
      });
    });

    it('should switch to favorited-articles tab', async () => {
      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeTruthy();
      });

      const event = new Event('click');
      fixture.componentInstance.selectTab(event, 'favorited-articles');

      expect(fixture.componentInstance.currentTab()).toBe('favorited-articles');
    });

    it('should prevent default event behavior when selecting tab', async () => {
      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeTruthy();
      });

      const event = new Event('click');
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      fixture.componentInstance.selectTab(event, 'my-articles');

      expect(preventDefaultSpy).toHaveBeenCalled();
    });
  });

  describe('isOwnProfile', () => {
    it('should return false when viewing another user profile', async () => {
      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.isOwnProfile()).toBe(false);
      });
    });

    it('should return true when viewing own profile', async () => {
      const { fixture } = await renderComponent('testuser', mockUser);

      await waitFor(() => {
        expect(fixture.componentInstance.isOwnProfile()).toBe(true);
      });
    });

    it('should return false when not logged in', async () => {
      const { fixture } = await renderComponent('otheruser', null);

      await waitFor(() => {
        expect(fixture.componentInstance.isOwnProfile()).toBe(false);
      });
    });
  });

  describe('Follow/Unfollow', () => {
    it('should follow user when not following', async () => {
      const followedProfile: ProfileResponse = {
        profile: { ...mockProfile, following: true },
      };
      mockProfileService.followUser.mockReturnValue(of(followedProfile));

      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeTruthy();
      });

      const event = new Event('click');
      fixture.componentInstance.toggleFollow(event);

      await waitFor(() => {
        expect(mockProfileService.followUser).toHaveBeenCalledWith('otheruser');
        expect(fixture.componentInstance.profile()?.following).toBe(true);
      });
    });

    it('should unfollow user when already following', async () => {
      const followedProfile: ProfileResponse = {
        profile: { ...mockProfile, following: true },
      };
      mockProfileService.getProfile.mockReturnValue(of(followedProfile));

      const unfollowedProfile: ProfileResponse = {
        profile: { ...mockProfile, following: false },
      };
      mockProfileService.unfollowUser.mockReturnValue(of(unfollowedProfile));

      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()?.following).toBe(true);
      });

      const event = new Event('click');
      fixture.componentInstance.toggleFollow(event);

      await waitFor(() => {
        expect(mockProfileService.unfollowUser).toHaveBeenCalledWith('otheruser');
        expect(fixture.componentInstance.profile()?.following).toBe(false);
      });
    });

    it('should prevent default event behavior when toggling follow', async () => {
      mockProfileService.followUser.mockReturnValue(of(mockProfileResponse));

      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeTruthy();
      });

      const event = new Event('click');
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      fixture.componentInstance.toggleFollow(event);

      expect(preventDefaultSpy).toHaveBeenCalled();
    });

    it('should handle follow error', async () => {
      mockProfileService.followUser.mockReturnValue(
        throwError(() => ({ message: 'Follow failed' }))
      );

      const consoleSpy = jest.spyOn(console, 'error').mockImplementation();

      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeTruthy();
      });

      const event = new Event('click');
      fixture.componentInstance.toggleFollow(event);

      await waitFor(() => {
        expect(consoleSpy).toHaveBeenCalledWith('Failed to toggle follow:', {
          message: 'Follow failed',
        });
      });

      consoleSpy.mockRestore();
    });

    it('should not call service if profile is null', async () => {
      mockProfileService.getProfile.mockReturnValue(of({ profile: null } as any));

      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeNull();
      });

      const event = new Event('click');
      fixture.componentInstance.toggleFollow(event);

      expect(mockProfileService.followUser).not.toHaveBeenCalled();
      expect(mockProfileService.unfollowUser).not.toHaveBeenCalled();
    });
  });

  describe('Edit Profile', () => {
    it('should navigate to settings page', async () => {
      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeTruthy();
      });

      const event = new Event('click');
      fixture.componentInstance.editProfile(event);

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/settings']);
    });

    it('should prevent default event behavior', async () => {
      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeTruthy();
      });

      const event = new Event('click');
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      fixture.componentInstance.editProfile(event);

      expect(preventDefaultSpy).toHaveBeenCalled();
    });
  });

  describe('Favorite Toggle', () => {
    it('should handle favorite toggle', async () => {
      const consoleSpy = jest.spyOn(console, 'log').mockImplementation();

      const { fixture } = await renderComponent();

      await waitFor(() => {
        expect(fixture.componentInstance.profile()).toBeTruthy();
      });

      fixture.componentInstance.onFavoriteToggle(mockArticle);

      expect(consoleSpy).toHaveBeenCalledWith('Favorite toggled for:', 'test-article');
      consoleSpy.mockRestore();
    });
  });
});
