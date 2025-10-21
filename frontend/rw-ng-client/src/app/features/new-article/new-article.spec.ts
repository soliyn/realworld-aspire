import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NewArticle } from './new-article';

describe('NewArticle', () => {
  let component: NewArticle;
  let fixture: ComponentFixture<NewArticle>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NewArticle]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NewArticle);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
