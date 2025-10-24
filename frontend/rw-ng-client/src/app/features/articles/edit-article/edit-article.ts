import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ArticlesService } from '../articles.service';

interface ArticleErrors {
  [key: string]: string[];
}

@Component({
  selector: 'app-edit-article',
  imports: [ReactiveFormsModule],
  templateUrl: './edit-article.html',
  styleUrl: './edit-article.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditArticle implements OnInit {
  private fb = inject(FormBuilder);
  private articlesService = inject(ArticlesService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  errors = signal<ArticleErrors | null>(null);
  isSubmitting = signal(false);
  isEditMode = signal(false);
  articleSlug = signal<string | null>(null);
  tagInput = signal('');

  articleForm = this.fb.nonNullable.group({
    title: ['', [Validators.required]],
    description: ['', [Validators.required]],
    body: ['', [Validators.required]],
    tagList: [[] as string[]],
  });

  ngOnInit(): void {
    // Check if we're in edit mode by looking for a slug in the route
    const slug = this.route.snapshot.paramMap.get('slug');

    if (slug) {
      this.isEditMode.set(true);
      this.articleSlug.set(slug);
      this.loadArticle(slug);
    }
  }

  loadArticle(slug: string): void {
    this.articlesService.getArticle(slug).subscribe({
      next: (response) => {
        const article = response.article;
        this.articleForm.patchValue({
          title: article.title,
          description: article.description,
          body: article.body,
          tagList: article.tagList,
        });
      },
      error: (error) => {
        this.errors.set(error.error?.errors || { error: ['Failed to load article'] });
        // Redirect to home if article not found
        this.router.navigate(['/']);
      },
    });
  }

  onSubmit(): void {
    if (this.articleForm.invalid || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errors.set(null);

    const formValue = this.articleForm.getRawValue();

    if (this.isEditMode() && this.articleSlug()) {
      // Update existing article
      this.articlesService.updateArticle(this.articleSlug()!, formValue).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.router.navigate(['/']);
        },
        error: (error) => {
          this.isSubmitting.set(false);
          this.errors.set(error.error?.errors || { error: ['Update failed'] });
        },
      });
    } else {
      // Create new article
      this.articlesService.createArticle(formValue).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.router.navigate(['/']);
        },
        error: (error) => {
          this.isSubmitting.set(false);
          this.errors.set(error.error?.errors || { error: ['Creation failed'] });
        },
      });
    }
  }

  addTag(): void {
    const tag = this.tagInput().trim();
    if (!tag) return;

    const currentTags = this.articleForm.controls.tagList.value;
    if (!currentTags.includes(tag)) {
      this.articleForm.controls.tagList.setValue([...currentTags, tag]);
    }
    this.tagInput.set('');
  }

  removeTag(tag: string): void {
    const currentTags = this.articleForm.controls.tagList.value;
    this.articleForm.controls.tagList.setValue(currentTags.filter((t) => t !== tag));
  }

  onTagInputKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.addTag();
    }
  }

  getErrorMessages(): string[] {
    const errorObj = this.errors();
    if (!errorObj) return [];

    return Object.entries(errorObj).map(([key, messages]) => {
      if (Array.isArray(messages)) {
        return `${key} ${messages.join(', ')}`;
      }
      return `${key}: ${messages}`;
    });
  }
}
