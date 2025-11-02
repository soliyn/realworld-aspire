import { ChangeDetectionStrategy, Component, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal, toObservable } from '@angular/core/rxjs-interop';
import { switchMap, map, catchError, of, startWith, merge, filter } from 'rxjs';
import { ProfileService } from './profile.service';
import { ArticlesService } from '../articles/articles.service';
import { AuthService } from '../../core/services/auth.service';
import { Profile as ProfileModel } from '../../core/models/profile.model';
import { Article } from '../../core/models/article.model';
import { ArticleListItem } from '../articles/article-list-item/article-list-item';
import { Subject } from 'rxjs';

type TabType = 'my-articles' | 'favorited-articles';

type ProfileState = {
  profile: ProfileModel | null;
  isLoading: boolean;
  error: string | null;
};

type ArticlesState = {
  articles: Article[];
  isLoading: boolean;
};

@Component({
  selector: 'app-profile',
  imports: [ArticleListItem],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Profile {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private profileService = inject(ProfileService);
  private articlesService = inject(ArticlesService);
  private authService = inject(AuthService);

  // Get username from route params
  private username = toSignal(
    this.route.params.pipe(map((params) => params['username'] as string)),
    { initialValue: '' }
  );

  // Current tab selection
  currentTab = signal<TabType>('my-articles');

  // Current user for authentication checks
  currentUser = toSignal(this.authService.currentUser$, { initialValue: null });

  // Subject to trigger follow/unfollow action
  private followAction$ = new Subject<{ username: string; shouldFollow: boolean }>();

  // Subject to trigger favorite/unfavorite action
  private favoriteAction$ = new Subject<{ slug: string; shouldFavorite: boolean }>();

  // Check if viewing own profile
  isOwnProfile = computed(() => {
    const user = this.currentUser();
    const profileUsername = this.username();
    return user?.username === profileUsername;
  });

  // Load profile data
  private profileState = toSignal(
    toObservable(this.username).pipe(
      switchMap((username) => {
        if (!username) {
          return of({ profile: null, isLoading: false, error: 'No username provided' });
        }

        // Filter follow actions for current username only
        const followActionsForUser$ = this.followAction$.pipe(
          filter(({ username: actionUsername }) => actionUsername === username),
          switchMap(({ shouldFollow }) => {
            return (
              shouldFollow
                ? this.profileService.followUser(username)
                : this.profileService.unfollowUser(username)
            ).pipe(
              map((response) => ({ profile: response.profile, isLoading: false, error: null })),
              catchError((error) => {
                return of({
                  profile: null,
                  isLoading: false,
                  error: error.message || 'Failed to toggle follow',
                });
              }),
              startWith({ profile: null, isLoading: true, error: null } as ProfileState)
            );
          })
        );

        return merge(
          this.profileService.getProfile(username).pipe(
            map((response) => ({ profile: response.profile, isLoading: false, error: null })),
            catchError((error) =>
              of({
                profile: null,
                isLoading: false,
                error: error.message || 'Failed to load profile',
              })
            ),
            startWith({ profile: null, isLoading: true, error: null } as ProfileState)
          ),
          followActionsForUser$
        );
      })
    ),
    { initialValue: { profile: null, isLoading: true, error: null } as ProfileState }
  );

  profile = computed(() => this.profileState().profile);
  isLoadingProfile = computed(() => this.profileState().isLoading);
  profileError = computed(() => this.profileState().error);

  // Load articles based on current tab and username
  private articlesParams = computed(() => ({
    username: this.username(),
    tab: this.currentTab(),
  }));

  // Signal to accumulate article updates from favorite actions
  private articleUpdatesMap = signal<Map<string, Article>>(new Map());

  // Track article updates from favorite actions using toSignal
  // We need to keep this signal alive to process favorite actions, even though we don't read its value
  private _favoriteUpdateEffect = toSignal(
    this.favoriteAction$.pipe(
      switchMap(({ slug, shouldFavorite }) => {
        return (
          shouldFavorite
            ? this.articlesService.favoriteArticle(slug)
            : this.articlesService.unfavoriteArticle(slug)
        ).pipe(
          map((response) => {
            // Update the accumulated map
            const currentMap = new Map(this.articleUpdatesMap());
            currentMap.set(slug, response.article);
            this.articleUpdatesMap.set(currentMap);
            return null;
          }),
          catchError((error) => {
            console.error('Error toggling favorite:', error);
            return of(null);
          })
        );
      })
    ),
    { initialValue: null }
  );

  private articlesState = toSignal(
    toObservable(this.articlesParams).pipe(
      switchMap(({ username, tab }) => {
        if (!username) {
          return of({ articles: [], isLoading: false });
        }

        const params = tab === 'my-articles' ? { author: username } : { favorited: username };

        // Clear article updates when params change
        this.articleUpdatesMap.set(new Map());

        return this.articlesService.getArticles(params).pipe(
          map((response) => ({ articles: response.articles, isLoading: false })),
          catchError(() => of({ articles: [], isLoading: false })),
          startWith({ articles: [], isLoading: true } as ArticlesState)
        );
      })
    ),
    { initialValue: { articles: [], isLoading: true } as ArticlesState }
  );

  articles = computed(() => {
    const baseArticles = this.articlesState().articles;
    const updates = this.articleUpdatesMap();

    if (updates.size === 0) {
      return baseArticles;
    }

    // Apply all accumulated updates
    return baseArticles.map((article) => updates.get(article.slug) || article);
  });

  isLoadingArticles = computed(() => this.articlesState().isLoading);

  selectTab(event: Event, tab: TabType): void {
    event.preventDefault();
    this.currentTab.set(tab);
  }

  toggleFollow(event: Event): void {
    event.preventDefault();
    const profileData = this.profile();
    const username = this.username();

    if (!profileData || !username) return;

    // Trigger follow/unfollow action via Subject
    this.followAction$.next({
      username,
      shouldFollow: !profileData.following,
    });
  }

  editProfile(event: Event): void {
    event.preventDefault();
    this.router.navigate(['/settings']);
  }

  onFavoriteToggle(article: Article): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    // Trigger favorite/unfavorite action via Subject
    this.favoriteAction$.next({
      slug: article.slug,
      shouldFavorite: !article.favorited,
    });
  }
}
