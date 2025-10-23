import { TestBed } from '@angular/core/testing';
import { HttpRequest, HttpEvent } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let authService: jest.Mocked<AuthService>;
  let mockRequest: HttpRequest<unknown>;
  let mockNext: jest.Mock<Observable<HttpEvent<unknown>>, [HttpRequest<unknown>]>;

  beforeEach(() => {
    const authServiceMock = {
      getToken: jest.fn(),
      isAuthenticated: jest.fn(),
      login: jest.fn(),
      logout: jest.fn(),
      currentUserValue: null,
      currentUser$: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: authServiceMock }],
    });

    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;

    // Create a mock HTTP request
    mockRequest = new HttpRequest('GET', '/api/articles');

    // Create a mock next handler
    mockNext = jest.fn((_req: HttpRequest<unknown>) => of({} as HttpEvent<unknown>));
  });

  it('should be created', () => {
    expect(authInterceptor).toBeTruthy();
  });

  describe('when user has a token', () => {
    const testToken = 'test-token-123';

    beforeEach(() => {
      authService.getToken.mockReturnValue(testToken);
    });

    it('should add Authorization header to the request', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      expect(mockNext).toHaveBeenCalled();
      const modifiedRequest = mockNext.mock.calls[0][0];
      expect(modifiedRequest.headers.has('Authorization')).toBe(true);
      expect(modifiedRequest.headers.get('Authorization')).toBe(`Token ${testToken}`);
    });

    it('should call getToken on AuthService', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      expect(authService.getToken).toHaveBeenCalled();
    });

    it('should pass the modified request to next handler', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      expect(mockNext).toHaveBeenCalledTimes(1);
      const modifiedRequest = mockNext.mock.calls[0][0];
      expect(modifiedRequest).not.toBe(mockRequest);
      expect(modifiedRequest.url).toBe(mockRequest.url);
    });

    it('should preserve original request URL', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      const modifiedRequest = mockNext.mock.calls[0][0];
      expect(modifiedRequest.url).toBe('/api/articles');
    });

    it('should preserve original request method', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      const modifiedRequest = mockNext.mock.calls[0][0];
      expect(modifiedRequest.method).toBe('GET');
    });

    it('should preserve existing headers', () => {
      const requestWithHeaders = mockRequest.clone({
        setHeaders: {
          'Content-Type': 'application/json',
          'X-Custom-Header': 'custom-value',
        },
      });

      TestBed.runInInjectionContext(() => authInterceptor(requestWithHeaders, mockNext));

      const modifiedRequest = mockNext.mock.calls[0][0];
      expect(modifiedRequest.headers.get('Content-Type')).toBe('application/json');
      expect(modifiedRequest.headers.get('X-Custom-Header')).toBe('custom-value');
      expect(modifiedRequest.headers.get('Authorization')).toBe(`Token ${testToken}`);
    });

    it('should work with POST requests', () => {
      const postRequest = new HttpRequest('POST', '/api/articles', { title: 'Test' });

      TestBed.runInInjectionContext(() => authInterceptor(postRequest, mockNext));

      const modifiedRequest = mockNext.mock.calls[0][0];
      expect(modifiedRequest.headers.get('Authorization')).toBe(`Token ${testToken}`);
      expect(modifiedRequest.method).toBe('POST');
      expect(modifiedRequest.body).toEqual({ title: 'Test' });
    });

    it('should work with PUT requests', () => {
      const putRequest = new HttpRequest('PUT', '/api/articles/slug', { title: 'Updated' });

      TestBed.runInInjectionContext(() => authInterceptor(putRequest, mockNext));

      const modifiedRequest = mockNext.mock.calls[0][0];
      expect(modifiedRequest.headers.get('Authorization')).toBe(`Token ${testToken}`);
      expect(modifiedRequest.method).toBe('PUT');
    });

    it('should work with DELETE requests', () => {
      const deleteRequest = new HttpRequest('DELETE', '/api/articles/slug');

      TestBed.runInInjectionContext(() => authInterceptor(deleteRequest, mockNext));

      const modifiedRequest = mockNext.mock.calls[0][0];
      expect(modifiedRequest.headers.get('Authorization')).toBe(`Token ${testToken}`);
      expect(modifiedRequest.method).toBe('DELETE');
    });
  });

  describe('when user does not have a token', () => {
    beforeEach(() => {
      authService.getToken.mockReturnValue(null);
    });

    it('should not add Authorization header to the request', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      expect(mockNext).toHaveBeenCalled();
      const requestPassedToNext = mockNext.mock.calls[0][0];
      expect(requestPassedToNext.headers.has('Authorization')).toBe(false);
    });

    it('should call getToken on AuthService', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      expect(authService.getToken).toHaveBeenCalled();
    });

    it('should pass the original request to next handler', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      expect(mockNext).toHaveBeenCalledTimes(1);
      const requestPassedToNext = mockNext.mock.calls[0][0];
      expect(requestPassedToNext).toBe(mockRequest);
    });

    it('should preserve original request URL', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      const requestPassedToNext = mockNext.mock.calls[0][0];
      expect(requestPassedToNext.url).toBe('/api/articles');
    });

    it('should preserve original request method', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      const requestPassedToNext = mockNext.mock.calls[0][0];
      expect(requestPassedToNext.method).toBe('GET');
    });

    it('should preserve existing headers', () => {
      const requestWithHeaders = mockRequest.clone({
        setHeaders: {
          'Content-Type': 'application/json',
          'X-Custom-Header': 'custom-value',
        },
      });

      TestBed.runInInjectionContext(() => authInterceptor(requestWithHeaders, mockNext));

      const requestPassedToNext = mockNext.mock.calls[0][0];
      expect(requestPassedToNext.headers.get('Content-Type')).toBe('application/json');
      expect(requestPassedToNext.headers.get('X-Custom-Header')).toBe('custom-value');
      expect(requestPassedToNext.headers.has('Authorization')).toBe(false);
    });
  });

  describe('when token is empty string', () => {
    beforeEach(() => {
      authService.getToken.mockReturnValue('');
    });

    it('should not add Authorization header to the request', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      const requestPassedToNext = mockNext.mock.calls[0][0];
      expect(requestPassedToNext.headers.has('Authorization')).toBe(false);
    });

    it('should pass the original request to next handler', () => {
      TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      const requestPassedToNext = mockNext.mock.calls[0][0];
      expect(requestPassedToNext).toBe(mockRequest);
    });
  });

  describe('return value', () => {
    it('should return the observable from next handler when token exists', () => {
      authService.getToken.mockReturnValue('test-token');
      const expectedObservable = of({} as HttpEvent<unknown>);
      mockNext.mockReturnValue(expectedObservable);

      const result = TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      expect(result).toBe(expectedObservable);
    });

    it('should return the observable from next handler when token does not exist', () => {
      authService.getToken.mockReturnValue(null);
      const expectedObservable = of({} as HttpEvent<unknown>);
      mockNext.mockReturnValue(expectedObservable);

      const result = TestBed.runInInjectionContext(() => authInterceptor(mockRequest, mockNext));

      expect(result).toBe(expectedObservable);
    });
  });
});
