import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth-guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  let authService: jest.Mocked<AuthService>;
  let router: jest.Mocked<Router>;
  let mockRoute: ActivatedRouteSnapshot;
  let mockState: RouterStateSnapshot;

  beforeEach(() => {
    const authServiceMock = {
      isAuthenticated: jest.fn(),
      login: jest.fn(),
      logout: jest.fn(),
      getToken: jest.fn(),
      currentUserValue: null,
      currentUser$: jest.fn(),
    };

    const routerMock = {
      navigate: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    });

    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;

    // Mock route and state
    mockRoute = {} as ActivatedRouteSnapshot;
    mockState = {
      url: '/protected-route',
    } as RouterStateSnapshot;
  });

  it('should be created', () => {
    expect(authGuard).toBeTruthy();
  });

  describe('when user is authenticated', () => {
    beforeEach(() => {
      authService.isAuthenticated.mockReturnValue(true);
    });

    it('should return true', () => {
      const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

      expect(result).toBe(true);
    });

    it('should not navigate to login', () => {
      TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should call isAuthenticated on AuthService', () => {
      TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

      expect(authService.isAuthenticated).toHaveBeenCalled();
    });
  });

  describe('when user is not authenticated', () => {
    beforeEach(() => {
      authService.isAuthenticated.mockReturnValue(false);
    });

    it('should return false', () => {
      const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

      expect(result).toBe(false);
    });

    it('should navigate to /login', () => {
      TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

      expect(router.navigate).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/protected-route' },
      });
    });

    it('should store the attempted URL as returnUrl query param', () => {
      const customState = {
        url: '/settings',
      } as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => authGuard(mockRoute, customState));

      expect(router.navigate).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/settings' },
      });
    });

    it('should call isAuthenticated on AuthService', () => {
      TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

      expect(authService.isAuthenticated).toHaveBeenCalled();
    });
  });

  describe('with different routes', () => {
    beforeEach(() => {
      authService.isAuthenticated.mockReturnValue(false);
    });

    it('should handle URL with query parameters', () => {
      const stateWithParams = {
        url: '/profile?tab=favorites',
      } as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => authGuard(mockRoute, stateWithParams));

      expect(router.navigate).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/profile?tab=favorites' },
      });
    });

    it('should handle URL with fragments', () => {
      const stateWithFragment = {
        url: '/article/test-slug#comments',
      } as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => authGuard(mockRoute, stateWithFragment));

      expect(router.navigate).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/article/test-slug#comments' },
      });
    });

    it('should handle root URL', () => {
      const rootState = {
        url: '/',
      } as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => authGuard(mockRoute, rootState));

      expect(router.navigate).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/' },
      });
    });
  });
});
