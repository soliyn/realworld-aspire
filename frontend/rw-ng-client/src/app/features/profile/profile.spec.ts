import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, BehaviorSubject } from 'rxjs';
import { Profile } from './profile';
import { ProfileService } from './profile.service';
import { ArticlesService } from '../articles/articles.service';
import { AuthService, User } from '../../core/services/auth.service';
import { Profile as ProfileModel, ProfileResponse } from '../../core/models/profile.model';
import { ArticlesResponse, Article } from '../../core/models/article.model';

describe('Profile', () => {
  let component: Profile;
  let fixture: ComponentFixture<Profile>;
  let profileService: jest.Mocked<ProfileService>;
  let articlesService: jest.Mocked<ArticlesService>;
  let authService: jest.Mocked<AuthService>;
  let router: jest.Mocked<Router>;
  let activatedRoute: any;

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

  let currentUserSubject: BehaviorSubject<User | null>;
  let paramsSubject: BehaviorSubject<{ username: string }>;

  beforeEach(async () => {
    currentUserSubject = new BehaviorSubject<User | null>(mockUser);
    paramsSubject = new BehaviorSubject({ username: 'otheruser' });
    const profileServiceMock = {
      getProfile: jest.fn(),
      followUser: jest.fn(),
      unfollowUser: jest.fn(),
    };

    const articlesServiceMock = {
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

    const authServiceMock = {
      currentUser$: currentUserSubject.asObservable(),
      currentUserValue: mockUser,
      login: jest.fn(),
      register: jest.fn(),
      logout: jest.fn(),
      isAuthenticated: jest.fn(),
      getToken: jest.fn(),
    };

    const routerMock = {
      navigate: jest.fn(),
    };

    activatedRoute = {
      params: paramsSubject.asObservable(),
    };

    await TestBed.configureTestingModule({
      imports: [Profile],
      providers: [
        { provide: ProfileService, useValue: profileServiceMock },
        { provide: ArticlesService, useValue: articlesServiceMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: ActivatedRoute, useValue: activatedRoute },
      ],
    }).compileComponents();

    profileService = TestBed.inject(ProfileService) as jest.Mocked<ProfileService>;
    articlesService = TestBed.inject(ArticlesService) as jest.Mocked<ArticlesService>;
    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;

    profileService.getProfile.mockReturnValue(of(mockProfileResponse));
    articlesService.getArticles.mockReturnValue(of(mockArticlesResponse));
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(Profile);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Profile Loading', () => {
    it('should load profile on init', (done) => {
      fixture.detectChanges();

      setTimeout(() => {
        expect(profileService.getProfile).toHaveBeenCalledWith('otheruser');
        expect(component.profile()).toEqual(mockProfile);
        expect(component.isLoadingProfile()).toBe(false);
        done();
      }, 0);
    });

    it('should handle profile loading error', (done) => {
      profileService.getProfile.mockReturnValue(
        throwError(() => ({ message: 'Profile not found' }))
      );

      fixture.detectChanges();

      setTimeout(() => {
        expect(component.profile()).toBeNull();
        expect(component.profileError()).toBe('Profile not found');
        done();
      }, 0);
    });

    it('should handle generic profile loading error', (done) => {
      profileService.getProfile.mockReturnValue(throwError(() => ({})));

      fixture.detectChanges();

      setTimeout(() => {
        expect(component.profileError()).toBe('Failed to load profile');
        done();
      }, 0);
    });
  });

  describe('Articles Loading', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should load user articles by default', (done) => {
      setTimeout(() => {
        expect(articlesService.getArticles).toHaveBeenCalledWith({ author: 'otheruser' });
        expect(component.articles()).toEqual([mockArticle]);
        expect(component.isLoadingArticles()).toBe(false);
        done();
      }, 0);
    });

    it('should load favorited articles when tab is changed', (done) => {
      articlesService.getArticles.mockClear();

      component.selectTab(new Event('click'), 'favorited-articles');
      fixture.detectChanges();

      setTimeout(() => {
        expect(articlesService.getArticles).toHaveBeenCalledWith({ favorited: 'otheruser' });
        done();
      }, 0);
    });

    it('should handle articles loading error', (done) => {
      // Reset the component to start fresh
      articlesService.getArticles.mockReturnValue(throwError(() => ({ message: 'Failed' })));
      fixture = TestBed.createComponent(Profile);
      component = fixture.componentInstance;
      fixture.detectChanges();

      setTimeout(() => {
        expect(component.articles()).toEqual([]);
        done();
      }, 0);
    });
  });

  describe('Tab Selection', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should start with my-articles tab selected', () => {
      expect(component.currentTab()).toBe('my-articles');
    });

    it('should switch to favorited-articles tab', () => {
      const event = new Event('click');
      component.selectTab(event, 'favorited-articles');

      expect(component.currentTab()).toBe('favorited-articles');
    });

    it('should prevent default event behavior when selecting tab', () => {
      const event = new Event('click');
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      component.selectTab(event, 'my-articles');

      expect(preventDefaultSpy).toHaveBeenCalled();
    });
  });

  describe('isOwnProfile', () => {
    it('should return false when viewing another user profile', (done) => {
      fixture.detectChanges();

      setTimeout(() => {
        expect(component.isOwnProfile()).toBe(false);
        done();
      }, 0);
    });

    it('should return true when viewing own profile', (done) => {
      paramsSubject.next({ username: 'testuser' });
      fixture.detectChanges();

      setTimeout(() => {
        expect(component.isOwnProfile()).toBe(true);
        done();
      }, 0);
    });

    it('should return false when not logged in', (done) => {
      currentUserSubject.next(null);
      fixture.detectChanges();

      setTimeout(() => {
        expect(component.isOwnProfile()).toBe(false);
        done();
      }, 0);
    });
  });

  describe('Follow/Unfollow', () => {
    it('should follow user when not following', (done) => {
      // Reset everything to ensure clean state
      paramsSubject.next({ username: 'otheruser' });
      currentUserSubject.next(mockUser);
      profileService.getProfile.mockReturnValue(of(mockProfileResponse));
      articlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

      fixture = TestBed.createComponent(Profile);
      component = fixture.componentInstance;
      fixture.detectChanges();

      setTimeout(() => {
        const followedProfile: ProfileResponse = {
          profile: { ...mockProfile, following: true },
        };
        profileService.followUser.mockReturnValue(of(followedProfile));

        const event = new Event('click');
        component.toggleFollow(event);

        setTimeout(() => {
          expect(profileService.followUser).toHaveBeenCalledWith('otheruser');
          expect(component.profile()?.following).toBe(true);
          done();
        }, 0);
      }, 0);
    });

    it('should unfollow user when already following', (done) => {
      // Reset everything to ensure clean state
      paramsSubject.next({ username: 'otheruser' });
      currentUserSubject.next(mockUser);
      const followedProfile: ProfileResponse = {
        profile: { ...mockProfile, following: true },
      };
      profileService.getProfile.mockReturnValue(of(followedProfile));
      articlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

      // Reset component with following profile
      fixture = TestBed.createComponent(Profile);
      component = fixture.componentInstance;
      fixture.detectChanges();

      setTimeout(() => {
        const unfollowedProfile: ProfileResponse = {
          profile: { ...mockProfile, following: false },
        };
        profileService.unfollowUser.mockReturnValue(of(unfollowedProfile));

        const event = new Event('click');
        component.toggleFollow(event);

        setTimeout(() => {
          expect(profileService.unfollowUser).toHaveBeenCalledWith('otheruser');
          expect(component.profile()?.following).toBe(false);
          done();
        }, 0);
      }, 0);
    });

    it('should prevent default event behavior when toggling follow', (done) => {
      // Reset everything to ensure clean state
      paramsSubject.next({ username: 'otheruser' });
      currentUserSubject.next(mockUser);
      profileService.getProfile.mockReturnValue(of(mockProfileResponse));
      articlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

      fixture = TestBed.createComponent(Profile);
      component = fixture.componentInstance;
      fixture.detectChanges();

      setTimeout(() => {
        const event = new Event('click');
        const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

        profileService.followUser.mockReturnValue(of(mockProfileResponse));
        component.toggleFollow(event);

        expect(preventDefaultSpy).toHaveBeenCalled();
        done();
      }, 0);
    });

    it('should handle follow error', (done) => {
      // Reset everything to ensure clean state
      paramsSubject.next({ username: 'otheruser' });
      currentUserSubject.next(mockUser);
      profileService.getProfile.mockReturnValue(of(mockProfileResponse));
      articlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

      fixture = TestBed.createComponent(Profile);
      component = fixture.componentInstance;
      fixture.detectChanges();

      setTimeout(() => {
        profileService.followUser.mockReturnValue(
          throwError(() => ({ message: 'Follow failed' }))
        );
        const consoleSpy = jest.spyOn(console, 'error').mockImplementation();

        const event = new Event('click');
        component.toggleFollow(event);

        setTimeout(() => {
          expect(consoleSpy).toHaveBeenCalledWith('Failed to toggle follow:', {
            message: 'Follow failed',
          });
          consoleSpy.mockRestore();
          done();
        }, 0);
      }, 0);
    });

    it('should not call service if profile is null', (done) => {
      // Reset everything to ensure clean state
      paramsSubject.next({ username: 'otheruser' });
      currentUserSubject.next(mockUser);
      profileService.getProfile.mockReturnValue(of({ profile: null } as any));
      articlesService.getArticles.mockReturnValue(of(mockArticlesResponse));

      // Reset component with null profile
      fixture = TestBed.createComponent(Profile);
      component = fixture.componentInstance;
      fixture.detectChanges();

      setTimeout(() => {
        const event = new Event('click');
        component.toggleFollow(event);

        expect(profileService.followUser).not.toHaveBeenCalled();
        expect(profileService.unfollowUser).not.toHaveBeenCalled();
        done();
      }, 0);
    });
  });

  describe('Edit Profile', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should navigate to settings page', () => {
      const event = new Event('click');
      component.editProfile(event);

      expect(router.navigate).toHaveBeenCalledWith(['/settings']);
    });

    it('should prevent default event behavior', () => {
      const event = new Event('click');
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      component.editProfile(event);

      expect(preventDefaultSpy).toHaveBeenCalled();
    });
  });

  describe('Favorite Toggle', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should handle favorite toggle', () => {
      const consoleSpy = jest.spyOn(console, 'log').mockImplementation();

      component.onFavoriteToggle(mockArticle);

      expect(consoleSpy).toHaveBeenCalledWith('Favorite toggled for:', 'test-article');
      consoleSpy.mockRestore();
    });
  });
});
