import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService, User } from './auth.service';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/api-endpoints';

describe('AuthService', () => {
  let service: AuthService;
  let apiService: jest.Mocked<ApiService>;
  let router: jest.Mocked<Router>;

  const mockUser: User = {
    username: 'testuser',
    email: 'test@example.com',
    token: 'test-token-123',
  };

  const mockLoginResponse = {
    user: mockUser,
  };

  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();

    const apiServiceMock = {
      get: jest.fn(),
      post: jest.fn(),
      put: jest.fn(),
      delete: jest.fn(),
      patch: jest.fn(),
    };

    const routerMock = {
      navigate: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: ApiService, useValue: apiServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    });

    service = TestBed.inject(AuthService);
    apiService = TestBed.inject(ApiService) as jest.Mocked<ApiService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;
  });

  afterEach(() => {
    // Clean up localStorage after each test
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('initialization', () => {
    it('should initialize with null user when localStorage is empty', () => {
      expect(service.currentUserValue).toBeNull();
    });

    it('should initialize with user from localStorage if exists', () => {
      // Clear and recreate TestBed with user already in localStorage
      TestBed.resetTestingModule();
      localStorage.setItem('currentUser', JSON.stringify(mockUser));

      const apiServiceMock = {
        get: jest.fn(),
        post: jest.fn(),
        put: jest.fn(),
        delete: jest.fn(),
        patch: jest.fn(),
      };

      const routerMock = {
        navigate: jest.fn(),
      };

      TestBed.configureTestingModule({
        providers: [
          AuthService,
          { provide: ApiService, useValue: apiServiceMock },
          { provide: Router, useValue: routerMock },
        ],
      });

      const newService = TestBed.inject(AuthService);

      expect(newService.currentUserValue).toEqual(mockUser);
    });

    it('should emit current user through observable', (done) => {
      service.currentUser$.subscribe((user) => {
        expect(user).toBeNull();
        done();
      });
    });
  });

  describe('login', () => {
    it('should call apiService.post with correct endpoint and credentials', () => {
      apiService.post.mockReturnValue(of(mockLoginResponse));

      service.login('test@example.com', 'password123').subscribe();

      expect(apiService.post).toHaveBeenCalledWith(API_ENDPOINTS.users.login, {
        user: { email: 'test@example.com', password: 'password123' },
      });
    });

    it('should return user data on successful login', (done) => {
      apiService.post.mockReturnValue(of(mockLoginResponse));

      service.login('test@example.com', 'password123').subscribe((user) => {
        expect(user).toEqual(mockUser);
        done();
      });
    });

    it('should store user in localStorage on successful login', (done) => {
      apiService.post.mockReturnValue(of(mockLoginResponse));

      service.login('test@example.com', 'password123').subscribe(() => {
        const storedUser = localStorage.getItem('currentUser');
        expect(storedUser).toBe(JSON.stringify(mockUser));
        done();
      });
    });

    it('should update currentUserSubject on successful login', (done) => {
      apiService.post.mockReturnValue(of(mockLoginResponse));

      service.login('test@example.com', 'password123').subscribe(() => {
        expect(service.currentUserValue).toEqual(mockUser);
        done();
      });
    });

    it('should emit user through currentUser$ observable on successful login', (done) => {
      apiService.post.mockReturnValue(of(mockLoginResponse));

      // Skip the initial null value and get the updated value
      let emissionCount = 0;
      service.currentUser$.subscribe((user) => {
        emissionCount++;
        if (emissionCount === 2) {
          expect(user).toEqual(mockUser);
          done();
        }
      });

      service.login('test@example.com', 'password123').subscribe();
    });

    it('should handle login error', (done) => {
      const errorResponse = { error: { errors: { 'email or password': ['is invalid'] } } };
      apiService.post.mockReturnValue(throwError(() => errorResponse));

      service.login('test@example.com', 'wrong-password').subscribe({
        error: (error) => {
          expect(error).toEqual(errorResponse);
          expect(localStorage.getItem('currentUser')).toBeNull();
          expect(service.currentUserValue).toBeNull();
          done();
        },
      });
    });
  });

  describe('logout', () => {
    beforeEach(() => {
      // Set up a logged-in state
      localStorage.setItem('currentUser', JSON.stringify(mockUser));
      service['currentUserSubject'].next(mockUser);
    });

    it('should remove user from localStorage', () => {
      service.logout();

      expect(localStorage.getItem('currentUser')).toBeNull();
    });

    it('should update currentUserSubject to null', () => {
      service.logout();

      expect(service.currentUserValue).toBeNull();
    });

    it('should navigate to /login', () => {
      service.logout();

      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should emit null through currentUser$ observable', (done) => {
      // Skip the initial mockUser value and get the null value after logout
      let emissionCount = 0;
      service.currentUser$.subscribe((user) => {
        emissionCount++;
        if (emissionCount === 2) {
          expect(user).toBeNull();
          done();
        }
      });

      service.logout();
    });
  });

  describe('isAuthenticated', () => {
    it('should return false when no user is logged in', () => {
      expect(service.isAuthenticated()).toBe(false);
    });

    it('should return false when user exists but has no token', () => {
      const userWithoutToken = { email: 'test@example.com', token: '', username: 'testuser' };
      service['currentUserSubject'].next(userWithoutToken);

      expect(service.isAuthenticated()).toBe(false);
    });

    it('should return true when user is logged in with a token', () => {
      service['currentUserSubject'].next(mockUser);

      expect(service.isAuthenticated()).toBe(true);
    });
  });

  describe('getToken', () => {
    it('should return null when no user is logged in', () => {
      expect(service.getToken()).toBeNull();
    });

    it('should return null when user exists but has no token', () => {
      const userWithoutToken = { email: 'test@example.com', token: '', username: 'testuser' };
      service['currentUserSubject'].next(userWithoutToken);

      expect(service.getToken()).toBeNull();
    });

    it('should return token when user is logged in', () => {
      service['currentUserSubject'].next(mockUser);

      expect(service.getToken()).toBe('test-token-123');
    });
  });

  describe('currentUserValue getter', () => {
    it('should return current value of currentUserSubject', () => {
      expect(service.currentUserValue).toBeNull();

      service['currentUserSubject'].next(mockUser);

      expect(service.currentUserValue).toEqual(mockUser);
    });
  });
});
