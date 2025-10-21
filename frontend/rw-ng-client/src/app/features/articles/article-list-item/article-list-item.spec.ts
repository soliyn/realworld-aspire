import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ArticleListItem } from './article-list-item';

describe('ArticleListItem', () => {
  let component: ArticleListItem;
  let fixture: ComponentFixture<ArticleListItem>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ArticleListItem]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ArticleListItem);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
