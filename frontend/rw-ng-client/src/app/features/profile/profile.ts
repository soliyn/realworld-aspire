import { Component, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal, toObservable } from '@angular/core/rxjs-interop';
import { switchMap, map, catchError, of, startWith, merge } from 'rxjs';
import { ProfileService } from './profile.service';
import { ArticlesService } from '../articles/articles.service';
import { AuthService } from '../../core/services/auth.service';
import { Profile as ProfileModel } from '../../core/models/profile.model';
import { Article } from '../../core/models/article.model';
import { ArticleListItem } from '../articles/article-list-item/article-list-item';
import { CommonModule } from '@angular/common';
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
  imports: [CommonModule, RouterLink, ArticleListItem],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
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

  // Subject to trigger profile reload after follow/unfollow
  private profileReload$ = new Subject<ProfileModel>();

  // Check if viewing own profile
  isOwnProfile = computed(() => {
    const user = this.currentUser();
    const profileUsername = this.username();
    return user?.username === profileUsername;
  });

  // Load profile data
  private profileState = toSignal(
    merge(
      toObservable(this.username).pipe(
        switchMap((username) => {
          if (!username) {
            return of({ profile: null, isLoading: false, error: 'No username provided' });
          }
          return this.profileService.getProfile(username).pipe(
            map((response) => ({ profile: response.profile, isLoading: false, error: null })),
            catchError((error) =>
              of({
                profile: null,
                isLoading: false,
                error: error.message || 'Failed to load profile',
              })
            ),
            startWith({ profile: null, isLoading: true, error: null } as ProfileState)
          );
        })
      ),
      this.profileReload$.pipe(map((profile) => ({ profile, isLoading: false, error: null })))
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

  private articlesState = toSignal(
    toObservable(this.articlesParams).pipe(
      switchMap(({ username, tab }) => {
        if (!username) {
          return of({ articles: [], isLoading: false });
        }

        const params = tab === 'my-articles' ? { author: username } : { favorited: username };

        return this.articlesService.getArticles(params).pipe(
          map((response) => ({ articles: response.articles, isLoading: false })),
          catchError(() => of({ articles: [], isLoading: false })),
          startWith({ articles: [], isLoading: true } as ArticlesState)
        );
      })
    ),
    { initialValue: { articles: [], isLoading: true } as ArticlesState }
  );

  articles = computed(() => this.articlesState().articles);
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

    const action = profileData.following
      ? this.profileService.unfollowUser(username)
      : this.profileService.followUser(username);

    action.subscribe({
      next: (response) => {
        // Trigger profile reload by emitting the updated profile
        this.profileReload$.next(response.profile);
      },
      error: (error) => {
        console.error('Failed to toggle follow:', error);
      },
    });
  }

  editProfile(event: Event): void {
    event.preventDefault();
    this.router.navigate(['/settings']);
  }

  onFavoriteToggle(article: Article): void {
    // Handle favorite toggle - to be implemented
    console.log('Favorite toggled for:', article.slug);
  }
}
