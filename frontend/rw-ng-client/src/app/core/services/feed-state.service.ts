import { Injectable, signal, computed } from '@angular/core';

export type FeedType = 'global' | 'your-feed' | 'tag';

@Injectable({
  providedIn: 'root',
})
export class FeedStateService {
  private feedType = signal<FeedType>('global');
  private selectedTag = signal<string | null>(null);

  currentFeedType = this.feedType.asReadonly();
  currentTag = this.selectedTag.asReadonly();

  isGlobalFeed = computed(() => this.feedType() === 'global');
  isYourFeed = computed(() => this.feedType() === 'your-feed');
  isTagFeed = computed(() => this.feedType() === 'tag');

  selectGlobalFeed(): void {
    this.feedType.set('global');
    this.selectedTag.set(null);
  }

  selectYourFeed(): void {
    this.feedType.set('your-feed');
    this.selectedTag.set(null);
  }

  selectTagFeed(tag: string): void {
    this.feedType.set('tag');
    this.selectedTag.set(tag);
  }
}
