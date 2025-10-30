import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from './auth.service';

describe('errorInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let authService: { logout: jest.Mock };

  beforeEach(() => {
    authService = {
      logout: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should call logout when receiving 401 response', () => {
    httpClient.get('/api/test').subscribe({
      next: () => fail('should have failed with 401'),
      error: (error) => {
        expect(error.status).toBe(401);
        expect(authService.logout).toHaveBeenCalled();
      },
    });

    const req = httpMock.expectOne('/api/test');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
  });

  it('should call logout when receiving 403 response', () => {
    httpClient.get('/api/test').subscribe({
      next: () => fail('should have failed with 403'),
      error: (error) => {
        expect(error.status).toBe(403);
        expect(authService.logout).toHaveBeenCalled();
      },
    });

    const req = httpMock.expectOne('/api/test');
    req.flush('Forbidden', { status: 403, statusText: 'Forbidden' });
  });

  it('should not call logout for other error status codes', () => {
    httpClient.get('/api/test').subscribe({
      next: () => fail('should have failed with 500'),
      error: (error) => {
        expect(error.status).toBe(500);
        expect(authService.logout).not.toHaveBeenCalled();
      },
    });

    const req = httpMock.expectOne('/api/test');
    req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
  });

  it('should not interfere with successful requests', () => {
    const testData = { data: 'test' };

    httpClient.get('/api/test').subscribe({
      next: (data) => {
        expect(data).toEqual(testData);
        expect(authService.logout).not.toHaveBeenCalled();
      },
      error: () => fail('should not have failed'),
    });

    const req = httpMock.expectOne('/api/test');
    req.flush(testData);
  });
});
