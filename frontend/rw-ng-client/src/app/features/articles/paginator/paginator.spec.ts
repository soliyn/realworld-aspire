import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Paginator } from './paginator';

describe('Paginator', () => {
  let component: Paginator;
  let fixture: ComponentFixture<Paginator>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Paginator],
    }).compileComponents();

    fixture = TestBed.createComponent(Paginator);
    component = fixture.componentInstance;
  });

  describe('Component initialization', () => {
    it('should create', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.detectChanges();
      expect(component).toBeTruthy();
    });

    it('should have default currentPage of 1', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.detectChanges();
      expect(component.currentPage()).toBe(1);
    });

    it('should have default itemsPerPage of 10', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.detectChanges();
      expect(component.itemsPerPage()).toBe(10);
    });
  });

  describe('totalPages computed signal', () => {
    it('should calculate total pages correctly', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();
      expect(component.totalPages()).toBe(5);
    });

    it('should round up for partial pages', () => {
      fixture.componentRef.setInput('totalCount', 47);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();
      expect(component.totalPages()).toBe(5);
    });

    it('should return 1 page when totalCount is less than itemsPerPage', () => {
      fixture.componentRef.setInput('totalCount', 5);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();
      expect(component.totalPages()).toBe(1);
    });

    it('should return 0 pages when totalCount is 0', () => {
      fixture.componentRef.setInput('totalCount', 0);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();
      expect(component.totalPages()).toBe(0);
    });

    it('should recalculate when totalCount changes', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();
      expect(component.totalPages()).toBe(5);

      fixture.componentRef.setInput('totalCount', 100);
      fixture.detectChanges();
      expect(component.totalPages()).toBe(10);
    });
  });

  describe('pages computed signal', () => {
    it('should generate array of page numbers', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();
      expect(component.pages()).toEqual([1, 2, 3, 4, 5]);
    });

    it('should generate single page array', () => {
      fixture.componentRef.setInput('totalCount', 5);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();
      expect(component.pages()).toEqual([1]);
    });

    it('should generate empty array for 0 pages', () => {
      fixture.componentRef.setInput('totalCount', 0);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();
      expect(component.pages()).toEqual([]);
    });
  });

  describe('onPageClick', () => {
    let mockEvent: Event;

    beforeEach(() => {
      mockEvent = new Event('click');
      jest.spyOn(mockEvent, 'preventDefault');
    });

    it('should emit pageChange event when clicking a different page', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.componentRef.setInput('currentPage', 1);
      fixture.detectChanges();

      const pageChangeSpy = jest.fn();
      component.pageChange.subscribe(pageChangeSpy);

      component.onPageClick(2, mockEvent);

      expect(pageChangeSpy).toHaveBeenCalledWith(2);
      expect(mockEvent.preventDefault).toHaveBeenCalled();
    });

    it('should not emit pageChange event when clicking current page', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.componentRef.setInput('currentPage', 2);
      fixture.detectChanges();

      const pageChangeSpy = jest.fn();
      component.pageChange.subscribe(pageChangeSpy);

      component.onPageClick(2, mockEvent);

      expect(pageChangeSpy).not.toHaveBeenCalled();
      expect(mockEvent.preventDefault).toHaveBeenCalled();
    });

    it('should not emit pageChange event for page less than 1', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.componentRef.setInput('currentPage', 1);
      fixture.detectChanges();

      const pageChangeSpy = jest.fn();
      component.pageChange.subscribe(pageChangeSpy);

      component.onPageClick(0, mockEvent);

      expect(pageChangeSpy).not.toHaveBeenCalled();
      expect(mockEvent.preventDefault).toHaveBeenCalled();
    });

    it('should not emit pageChange event for page greater than totalPages', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.componentRef.setInput('currentPage', 1);
      fixture.detectChanges();

      const pageChangeSpy = jest.fn();
      component.pageChange.subscribe(pageChangeSpy);

      component.onPageClick(6, mockEvent);

      expect(pageChangeSpy).not.toHaveBeenCalled();
      expect(mockEvent.preventDefault).toHaveBeenCalled();
    });

    it('should always call preventDefault', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.componentRef.setInput('currentPage', 1);
      fixture.detectChanges();

      component.onPageClick(2, mockEvent);
      expect(mockEvent.preventDefault).toHaveBeenCalled();
    });
  });

  describe('Template rendering', () => {
    it('should render pagination when totalPages > 1', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();

      const pagination = fixture.nativeElement.querySelector('.pagination');
      expect(pagination).toBeTruthy();
    });

    it('should not render pagination when totalPages <= 1', () => {
      fixture.componentRef.setInput('totalCount', 5);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();

      const pagination = fixture.nativeElement.querySelector('.pagination');
      expect(pagination).toBeFalsy();
    });

    it('should render correct number of page items', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();

      const pageItems = fixture.nativeElement.querySelectorAll('.page-item');
      expect(pageItems.length).toBe(5);
    });

    it('should apply active class to current page', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.componentRef.setInput('currentPage', 3);
      fixture.detectChanges();

      const pageItems = fixture.nativeElement.querySelectorAll('.page-item');
      expect(pageItems[2].classList.contains('active')).toBe(true);
      expect(pageItems[0].classList.contains('active')).toBe(false);
      expect(pageItems[1].classList.contains('active')).toBe(false);
    });

    it('should display correct page numbers', () => {
      fixture.componentRef.setInput('totalCount', 30);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();

      const pageLinks = fixture.nativeElement.querySelectorAll('.page-link');
      expect(pageLinks[0].textContent.trim()).toBe('1');
      expect(pageLinks[1].textContent.trim()).toBe('2');
      expect(pageLinks[2].textContent.trim()).toBe('3');
    });

    it('should trigger onPageClick when page link is clicked', () => {
      fixture.componentRef.setInput('totalCount', 50);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.componentRef.setInput('currentPage', 1);
      fixture.detectChanges();

      jest.spyOn(component, 'onPageClick');

      const pageLinks = fixture.nativeElement.querySelectorAll('.page-link');
      pageLinks[1].click();

      expect(component.onPageClick).toHaveBeenCalled();
    });
  });

  describe('Edge cases', () => {
    it('should handle exactly one page of items', () => {
      fixture.componentRef.setInput('totalCount', 10);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();

      expect(component.totalPages()).toBe(1);
      const pagination = fixture.nativeElement.querySelector('.pagination');
      expect(pagination).toBeFalsy();
    });

    it('should handle large number of items', () => {
      fixture.componentRef.setInput('totalCount', 1000);
      fixture.componentRef.setInput('itemsPerPage', 10);
      fixture.detectChanges();

      expect(component.totalPages()).toBe(100);
      expect(component.pages().length).toBe(100);
    });

    it('should handle different itemsPerPage values', () => {
      fixture.componentRef.setInput('totalCount', 100);
      fixture.componentRef.setInput('itemsPerPage', 20);
      fixture.detectChanges();

      expect(component.totalPages()).toBe(5);
      expect(component.pages()).toEqual([1, 2, 3, 4, 5]);
    });
  });
});
