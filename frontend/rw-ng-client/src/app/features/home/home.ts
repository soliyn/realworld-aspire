import { Component, inject } from '@angular/core';
import { TagList } from '../tag-list/tag-list';
import { ArticleList } from '../articles/article-list/article-list';
import { FeedStateService } from '../../core/services/feed-state.service';

@Component({
  selector: 'app-home',
  imports: [TagList, ArticleList],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private feedStateService = inject(FeedStateService);

  currentFeedType = this.feedStateService.currentFeedType;
  currentTag = this.feedStateService.currentTag;

  selectYourFeed(event: Event): void {
    event.preventDefault();
    this.feedStateService.selectYourFeed();
  }

  selectGlobalFeed(event: Event): void {
    event.preventDefault();
    this.feedStateService.selectGlobalFeed();
  }
}
