import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError, Observable } from 'rxjs';
import { TagList } from './tag-list';
import { TagsService, TagsResponse } from './tags.service';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';

describe('TagList', () => {
  let component: TagList;
  let fixture: ComponentFixture<TagList>;
  let tagsService: jest.Mocked<TagsService>;

  const mockTagsResponse: TagsResponse = {
    tags: ['angular', 'typescript', 'testing', 'rxjs'],
  };

  beforeEach(async () => {
    const tagsServiceMock = {
      getTags: jest.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [TagList],
      providers: [{ provide: TagsService, useValue: tagsServiceMock }],
    }).compileComponents();

    tagsService = TestBed.inject(TagsService) as jest.Mocked<TagsService>;
    tagsService.getTags.mockReturnValue(of(mockTagsResponse));

    fixture = TestBed.createComponent(TagList);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize tags signal with data from service', () => {
    // toSignal executes immediately on component creation
    expect(component.tags()).toEqual(mockTagsResponse.tags);
  });

  it('should call TagsService.getTags on component initialization', () => {
    fixture.detectChanges();
    expect(tagsService.getTags).toHaveBeenCalledTimes(1);
  });

  it('should populate tags signal with data from service', () => {
    fixture.detectChanges();
    expect(component.tags()).toEqual(mockTagsResponse.tags);
  });

  it('should render all tags in the template', () => {
    fixture.detectChanges();

    const tagElements: DebugElement[] = fixture.debugElement.queryAll(
      By.css('.tag-pill')
    );

    expect(tagElements.length).toBe(4);
    expect(tagElements[0].nativeElement.textContent.trim()).toBe('angular');
    expect(tagElements[1].nativeElement.textContent.trim()).toBe('typescript');
    expect(tagElements[2].nativeElement.textContent.trim()).toBe('testing');
    expect(tagElements[3].nativeElement.textContent.trim()).toBe('rxjs');
  });

  it('should display loading message while tags are being fetched', () => {
    // Create a new fixture with a delayed observable
    const delayedResponse = new Promise<TagsResponse>((resolve) => {
      setTimeout(() => resolve(mockTagsResponse), 100);
    });

    tagsService.getTags.mockReturnValue(
      new Observable<TagsResponse>((subscriber) => {
        delayedResponse.then((response) => {
          subscriber.next(response);
          subscriber.complete();
        });
      })
    );

    const delayedFixture = TestBed.createComponent(TagList);
    const delayedComponent = delayedFixture.componentInstance;

    // Before detectChanges, the signal subscription hasn't started
    expect(delayedComponent.isLoading()).toBe(true);

    delayedFixture.detectChanges();

    const loadingMessage: DebugElement = delayedFixture.debugElement.query(
      By.css('.tag-list div')
    );

    expect(loadingMessage.nativeElement.textContent.trim()).toBe('Loading tags...');
  });

  it('should handle empty tags response without showing loading message', () => {
    tagsService.getTags.mockReturnValue(of({ tags: [] }));

    fixture = TestBed.createComponent(TagList);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.tags()).toEqual([]);
    expect(component.isLoading()).toBe(false);

    const tagList: DebugElement = fixture.debugElement.query(By.css('.tag-list'));
    const tagPills = fixture.debugElement.queryAll(By.css('.tag-pill'));

    expect(tagPills.length).toBe(0);
    expect(tagList.nativeElement.textContent.trim()).toBe('');
  });

  it('should render tags with correct CSS classes', () => {
    fixture.detectChanges();

    const tagElements: DebugElement[] = fixture.debugElement.queryAll(
      By.css('.tag-pill.tag-default')
    );

    expect(tagElements.length).toBe(4);
    tagElements.forEach((element) => {
      expect(element.nativeElement.classList.contains('tag-pill')).toBe(true);
      expect(element.nativeElement.classList.contains('tag-default')).toBe(true);
    });
  });

  it('should display "Popular Tags" heading', () => {
    fixture.detectChanges();

    const heading: DebugElement = fixture.debugElement.query(By.css('p'));
    expect(heading.nativeElement.textContent).toBe('Popular Tags');
  });

  it('should set isLoading to false after tags are loaded', () => {
    fixture.detectChanges();

    expect(component.isLoading()).toBe(false);
    expect(component.tags()).toEqual(mockTagsResponse.tags);
  });

  it('should handle service errors gracefully', () => {
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();

    tagsService.getTags.mockReturnValue(
      throwError(() => new Error('API Error'))
    );

    const errorFixture = TestBed.createComponent(TagList);
    const errorComponent = errorFixture.componentInstance;
    errorFixture.detectChanges();

    // Should return empty array on error
    expect(errorComponent.tags()).toEqual([]);
    // Should set loading to false
    expect(errorComponent.isLoading()).toBe(false);
    // Should log the error
    expect(consoleErrorSpy).toHaveBeenCalledWith(
      'Error loading tags:',
      expect.any(Error)
    );

    consoleErrorSpy.mockRestore();
  });

  it('should track tags by tag value in @for loop', () => {
    fixture.detectChanges();

    const tagElements: DebugElement[] = fixture.debugElement.queryAll(
      By.css('.tag-pill')
    );

    const firstTagText = tagElements[0].nativeElement.textContent.trim();
    expect(firstTagText).toBe('angular');

    // Update with new tags
    const newTags: TagsResponse = {
      tags: ['angular', 'react', 'vue'],
    };
    tagsService.getTags.mockReturnValue(of(newTags));

    fixture = TestBed.createComponent(TagList);
    fixture.detectChanges();

    const updatedTagElements: DebugElement[] = fixture.debugElement.queryAll(
      By.css('.tag-pill')
    );

    expect(updatedTagElements.length).toBe(3);
    expect(updatedTagElements[0].nativeElement.textContent.trim()).toBe('angular');
    expect(updatedTagElements[1].nativeElement.textContent.trim()).toBe('react');
    expect(updatedTagElements[2].nativeElement.textContent.trim()).toBe('vue');
  });
});
