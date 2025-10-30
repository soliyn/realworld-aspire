import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { EditArticle } from './edit-article';
import { ArticlesService } from '../articles.service';
import { ArticleResponse } from '../../../core/models/article.model';

describe('EditArticle', () => {
  let component: EditArticle;
  let fixture: ComponentFixture<EditArticle>;
  let articlesService: jest.Mocked<ArticlesService>;
  let router: jest.Mocked<Router>;
  let activatedRoute: Partial<ActivatedRoute>;

  const mockArticleResponse: ArticleResponse = {
    article: {
      slug: 'test-article',
      title: 'Test Article',
      description: 'Test description',
      body: 'Test body content',
      tagList: ['test', 'angular'],
      createdAt: '2024-01-01',
      updatedAt: '2024-01-01',
      favorited: false,
      favoritesCount: 0,
      author: {
        username: 'testuser',
        bio: '',
        image: '',
        following: false,
      },
    },
  };

  beforeEach(async () => {
    const articlesServiceMock = {
      getArticle: jest.fn(),
      createArticle: jest.fn(),
      updateArticle: jest.fn(),
    };

    const routerMock = {
      navigate: jest.fn(),
    };

    activatedRoute = {
      snapshot: {
        paramMap: {
          get: jest.fn().mockReturnValue(null),
        },
      } as any,
    };

    await TestBed.configureTestingModule({
      imports: [EditArticle],
      providers: [
        { provide: ArticlesService, useValue: articlesServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: ActivatedRoute, useValue: activatedRoute },
      ],
    }).compileComponents();

    articlesService = TestBed.inject(ArticlesService) as jest.Mocked<ArticlesService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;

    fixture = TestBed.createComponent(EditArticle);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  describe('Create mode', () => {
    beforeEach(() => {
      activatedRoute.snapshot!.paramMap.get = jest.fn().mockReturnValue(null);
    });

    it('should initialize in create mode when no slug is provided', () => {
      fixture.detectChanges();

      expect(component.isEditMode()).toBe(false);
      expect(component.articleSlug()).toBeNull();
    });

    it('should have an empty form initially', () => {
      fixture.detectChanges();

      expect(component.articleForm.value).toEqual({
        title: '',
        description: '',
        body: '',
        tagList: [],
      });
    });

    it('should create a new article on submit', () => {
      articlesService.createArticle.mockReturnValue(of(mockArticleResponse));
      fixture.detectChanges();

      component.articleForm.patchValue({
        title: 'New Article',
        description: 'New description',
        body: 'New body',
        tagList: ['test'],
      });

      component.onSubmit();

      expect(articlesService.createArticle).toHaveBeenCalledWith({
        title: 'New Article',
        description: 'New description',
        body: 'New body',
        tagList: ['test'],
      });
      expect(router.navigate).toHaveBeenCalledWith(['/']);
    });

    it('should set errors when article creation fails', () => {
      const error = {
        error: {
          errors: { title: ['is required'] },
        },
      };
      articlesService.createArticle.mockReturnValue(throwError(() => error));
      fixture.detectChanges();

      component.articleForm.patchValue({
        title: 'New Article',
        description: 'New description',
        body: 'New body',
      });

      component.onSubmit();

      expect(component.errors()).toEqual({ title: ['is required'] });
      expect(component.isSubmitting()).toBe(false);
    });

    it('should not submit if form is invalid', () => {
      fixture.detectChanges();

      // Form is empty, so invalid
      component.onSubmit();

      expect(articlesService.createArticle).not.toHaveBeenCalled();
    });

    it('should not submit if already submitting', () => {
      fixture.detectChanges();

      component.isSubmitting.set(true);
      component.articleForm.patchValue({
        title: 'Test',
        description: 'Test',
        body: 'Test',
      });

      component.onSubmit();

      expect(articlesService.createArticle).not.toHaveBeenCalled();
    });
  });

  describe('Edit mode', () => {
    beforeEach(() => {
      activatedRoute.snapshot!.paramMap.get = jest.fn().mockReturnValue('test-article');
    });

    it('should initialize in edit mode when slug is provided', () => {
      articlesService.getArticle.mockReturnValue(of(mockArticleResponse));
      fixture.detectChanges();

      expect(component.isEditMode()).toBe(true);
      expect(component.articleSlug()).toBe('test-article');
    });

    it('should load article data on init', () => {
      articlesService.getArticle.mockReturnValue(of(mockArticleResponse));
      fixture.detectChanges();

      expect(articlesService.getArticle).toHaveBeenCalledWith('test-article');
      expect(component.articleForm.value).toEqual({
        title: 'Test Article',
        description: 'Test description',
        body: 'Test body content',
        tagList: ['test', 'angular'],
      });
    });

    it('should handle error when loading article fails', () => {
      const error = {
        error: {
          errors: { article: ['not found'] },
        },
      };
      articlesService.getArticle.mockReturnValue(throwError(() => error));
      fixture.detectChanges();

      expect(component.errors()).toEqual({ article: ['not found'] });
      expect(router.navigate).toHaveBeenCalledWith(['/']);
    });

    it('should update existing article on submit', () => {
      articlesService.getArticle.mockReturnValue(of(mockArticleResponse));
      articlesService.updateArticle.mockReturnValue(of(mockArticleResponse));
      fixture.detectChanges();

      component.articleForm.patchValue({
        title: 'Updated Title',
      });

      component.onSubmit();

      expect(articlesService.updateArticle).toHaveBeenCalledWith('test-article', {
        title: 'Updated Title',
        description: 'Test description',
        body: 'Test body content',
        tagList: ['test', 'angular'],
      });
      expect(router.navigate).toHaveBeenCalledWith(['/']);
    });

    it('should set errors when article update fails', () => {
      const error = {
        error: {
          errors: { title: ['is too long'] },
        },
      };
      articlesService.getArticle.mockReturnValue(of(mockArticleResponse));
      articlesService.updateArticle.mockReturnValue(throwError(() => error));
      fixture.detectChanges();

      component.onSubmit();

      expect(component.errors()).toEqual({ title: ['is too long'] });
      expect(component.isSubmitting()).toBe(false);
    });
  });

  describe('Tag management', () => {
    beforeEach(() => {
      activatedRoute.snapshot!.paramMap.get = jest.fn().mockReturnValue(null);
      fixture.detectChanges();
    });

    it('should add a tag when addTag is called', () => {
      component.tagInput.set('newtag');
      component.addTag();

      expect(component.articleForm.controls.tagList.value).toEqual(['newtag']);
      expect(component.tagInput()).toBe('');
    });

    it('should not add empty tag', () => {
      component.tagInput.set('   ');
      component.addTag();

      expect(component.articleForm.controls.tagList.value).toEqual([]);
    });

    it('should not add duplicate tag', () => {
      component.articleForm.controls.tagList.setValue(['existing']);
      component.tagInput.set('existing');
      component.addTag();

      expect(component.articleForm.controls.tagList.value).toEqual(['existing']);
    });

    it('should remove a tag', () => {
      component.articleForm.controls.tagList.setValue(['tag1', 'tag2', 'tag3']);
      component.removeTag('tag2');

      expect(component.articleForm.controls.tagList.value).toEqual(['tag1', 'tag3']);
    });

    it('should add tag on Enter key press', () => {
      component.tagInput.set('newtag');
      const event = new KeyboardEvent('keydown', { key: 'Enter' });
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      component.onTagInputKeydown(event);

      expect(preventDefaultSpy).toHaveBeenCalled();
      expect(component.articleForm.controls.tagList.value).toEqual(['newtag']);
    });

    it('should not add tag on other key press', () => {
      component.tagInput.set('newtag');
      const event = new KeyboardEvent('keydown', { key: 'a' });

      component.onTagInputKeydown(event);

      expect(component.articleForm.controls.tagList.value).toEqual([]);
      expect(component.tagInput()).toBe('newtag');
    });
  });

  describe('Error messages', () => {
    beforeEach(() => {
      activatedRoute.snapshot!.paramMap.get = jest.fn().mockReturnValue(null);
      fixture.detectChanges();
    });

    it('should return empty array when no errors', () => {
      expect(component.getErrorMessages()).toEqual([]);
    });

    it('should format error messages correctly', () => {
      component.errors.set({
        title: ['is required', 'is too short'],
        description: ['is required'],
      });

      const messages = component.getErrorMessages();

      expect(messages).toContain('title is required, is too short');
      expect(messages).toContain('description is required');
    });
  });
});
