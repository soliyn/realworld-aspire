import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';
import { marked } from 'marked';
import { ArticlesService } from '../articles.service';
import { AuthService } from '../../../core/services/auth.service';
import { Article } from '../../../core/models/article.model';
import { Comment } from '../../../core/models/comment.model';

@Component({
  selector: 'app-view-article',
  imports: [DatePipe, RouterLink, ReactiveFormsModule],
  templateUrl: './view-article.html',
  styleUrl: './view-article.scss',
})
export class ViewArticle implements OnInit {
  private articlesService = inject(ArticlesService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  // State
  article = signal<Article | null>(null);
  comments = signal<Comment[]>([]);
  isLoadingArticle = signal(true);
  isLoadingComments = signal(true);
  isDeletingArticle = signal(false);
  isSubmittingComment = signal(false);
  isDeletingComment = signal<number | null>(null);
  articleError = signal<string | null>(null);
  commentsError = signal<string | null>(null);

  // Form control for new comment
  commentControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(1)],
  });

  // Current user
  currentUser = toSignal(this.authService.currentUser$);

  // Computed properties
  isAuthenticated = computed(() => !!this.currentUser());

  isAuthor = computed(() => {
    const article = this.article();
    const user = this.currentUser();
    return article && user ? article.author.username === user.username : false;
  });

  canModifyArticle = computed(() => this.isAuthenticated() && this.isAuthor());

  canModifyComment = (comment: Comment) => {
    const user = this.currentUser();
    return user && comment.author.username === user.username;
  };

  // Parse markdown body to HTML
  articleBodyHtml = computed(() => {
    const article = this.article();
    if (!article || !article.body) {
      return '';
    }
    return marked.parse(article.body, { async: false }) as string;
  });

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.router.navigate(['/']);
      return;
    }

    this.loadArticle(slug);
    this.loadComments(slug);
  }

  private loadArticle(slug: string): void {
    this.isLoadingArticle.set(true);
    this.articleError.set(null);

    this.articlesService
      .getArticle(slug)
      .pipe(
        tap((response) => {
          this.article.set(response.article);
          this.isLoadingArticle.set(false);
        }),
        catchError((error) => {
          console.error('Error loading article:', error);
          this.articleError.set('Failed to load article. It may not exist.');
          this.isLoadingArticle.set(false);
          return of(null);
        })
      )
      .subscribe();
  }

  private loadComments(slug: string): void {
    this.isLoadingComments.set(true);
    this.commentsError.set(null);

    this.articlesService
      .getComments(slug)
      .pipe(
        tap((response) => {
          this.comments.set(response.comments);
          this.isLoadingComments.set(false);
        }),
        catchError((error) => {
          console.error('Error loading comments:', error);
          this.commentsError.set('Failed to load comments.');
          this.isLoadingComments.set(false);
          return of({ comments: [] });
        })
      )
      .subscribe();
  }

  onToggleFavorite(): void {
    const article = this.article();
    if (!article || !this.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    const operation = article.favorited
      ? this.articlesService.unfavoriteArticle(article.slug)
      : this.articlesService.favoriteArticle(article.slug);

    operation
      .pipe(
        tap((response) => {
          this.article.set(response.article);
        }),
        catchError((error) => {
          console.error('Error toggling favorite:', error);
          return of(null);
        })
      )
      .subscribe();
  }

  onToggleFollow(): void {
    const article = this.article();
    if (!article || !this.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    // Note: ProfileService would handle this, but since we don't have it,
    // we'll need to add it when profile service is available
    console.log('Toggle follow for:', article.author.username);
  }

  onEditArticle(): void {
    const article = this.article();
    if (article && this.canModifyArticle()) {
      this.router.navigate(['/editor', article.slug]);
    }
  }

  onDeleteArticle(): void {
    const article = this.article();
    if (!article || !this.canModifyArticle()) {
      return;
    }

    if (!confirm('Are you sure you want to delete this article?')) {
      return;
    }

    this.isDeletingArticle.set(true);

    this.articlesService
      .deleteArticle(article.slug)
      .pipe(
        tap(() => {
          this.router.navigate(['/']);
        }),
        catchError((error) => {
          console.error('Error deleting article:', error);
          this.isDeletingArticle.set(false);
          alert('Failed to delete article. Please try again.');
          return of(null);
        })
      )
      .subscribe();
  }

  onSubmitComment(): void {
    const article = this.article();
    if (!article || !this.isAuthenticated() || this.commentControl.invalid) {
      return;
    }

    const commentBody = this.commentControl.value.trim();
    if (!commentBody) {
      return;
    }

    this.isSubmittingComment.set(true);

    this.articlesService
      .addComment(article.slug, { body: commentBody })
      .pipe(
        tap((response) => {
          // Add the new comment to the top of the list
          this.comments.update((comments) => [response.comment, ...comments]);
          // Clear the form
          this.commentControl.reset();
          this.isSubmittingComment.set(false);
        }),
        catchError((error) => {
          console.error('Error adding comment:', error);
          this.isSubmittingComment.set(false);
          alert('Failed to add comment. Please try again.');
          return of(null);
        })
      )
      .subscribe();
  }

  onDeleteComment(commentId: number): void {
    const article = this.article();
    if (!article) {
      return;
    }

    if (!confirm('Are you sure you want to delete this comment?')) {
      return;
    }

    this.isDeletingComment.set(commentId);

    this.articlesService
      .deleteComment(article.slug, commentId)
      .pipe(
        tap(() => {
          // Remove the comment from the list
          this.comments.update((comments) =>
            comments.filter((comment) => comment.id !== commentId)
          );
          this.isDeletingComment.set(null);
        }),
        catchError((error) => {
          console.error('Error deleting comment:', error);
          this.isDeletingComment.set(null);
          alert('Failed to delete comment. Please try again.');
          return of(null);
        })
      )
      .subscribe();
  }
}
