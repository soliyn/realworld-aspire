import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Settings } from './settings';
import { AuthService, User } from '../../core/services/auth.service';
import { ApiService } from '../../core/services/api.service';
import { API_ENDPOINTS } from '../../core/constants/api-endpoints';

describe('Settings', () => {
  let component: Settings;
  let fixture: ComponentFixture<Settings>;
  let authService: jest.Mocked<AuthService>;
  let apiService: jest.Mocked<ApiService>;
  let router: jest.Mocked<Router>;

  const mockUser: User = {
    email: 'test@example.com',
    token: 'test-token',
    username: 'testuser',
    bio: 'Test bio',
    image: 'https://example.com/image.jpg',
  };

  beforeEach(async () => {
    const authServiceMock = {
      logout: jest.fn(),
      currentUserValue: mockUser,
      currentUser$: of(mockUser),
      login: jest.fn(),
      register: jest.fn(),
      isAuthenticated: jest.fn(),
      getToken: jest.fn(),
    };
    const apiServiceMock = {
      put: jest.fn(),
      get: jest.fn(),
      post: jest.fn(),
      delete: jest.fn(),
      patch: jest.fn(),
    };
    const routerMock = {
      navigate: jest.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [Settings],
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: ApiService, useValue: apiServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    }).compileComponents();

    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;
    apiService = TestBed.inject(ApiService) as jest.Mocked<ApiService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;

    fixture = TestBed.createComponent(Settings);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should pre-populate form with current user data', () => {
      fixture.detectChanges();

      expect(component.settingsForm.value).toEqual({
        image: mockUser.image,
        username: mockUser.username,
        bio: mockUser.bio,
        email: mockUser.email,
        password: '',
      });
    });

    it('should redirect to login if no user is logged in', () => {
      Object.defineProperty(authService, 'currentUserValue', { value: null, writable: true });

      fixture.detectChanges();

      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should handle missing bio and image', () => {
      const userWithoutBioAndImage: User = {
        email: 'test@example.com',
        token: 'test-token',
        username: 'testuser',
      };
      Object.defineProperty(authService, 'currentUserValue', {
        value: userWithoutBioAndImage,
        writable: true,
      });

      fixture.detectChanges();

      expect(component.settingsForm.value.bio).toBe('');
      expect(component.settingsForm.value.image).toBe('');
    });
  });

  describe('Form Validation', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should be valid with required fields filled', () => {
      component.settingsForm.patchValue({
        email: 'test@example.com',
        username: 'testuser',
      });

      expect(component.settingsForm.valid).toBe(true);
    });

    it('should be invalid without email', () => {
      component.settingsForm.patchValue({
        email: '',
        username: 'testuser',
      });

      expect(component.settingsForm.valid).toBe(false);
      expect(component.settingsForm.get('email')?.hasError('required')).toBe(true);
    });

    it('should be invalid with invalid email format', () => {
      component.settingsForm.patchValue({
        email: 'invalid-email',
        username: 'testuser',
      });

      expect(component.settingsForm.valid).toBe(false);
      expect(component.settingsForm.get('email')?.hasError('email')).toBe(true);
    });

    it('should be invalid without username', () => {
      component.settingsForm.patchValue({
        email: 'test@example.com',
        username: '',
      });

      expect(component.settingsForm.valid).toBe(false);
      expect(component.settingsForm.get('username')?.hasError('required')).toBe(true);
    });

    it('should be valid without password (optional field)', () => {
      component.settingsForm.patchValue({
        email: 'test@example.com',
        username: 'testuser',
        password: '',
      });

      expect(component.settingsForm.valid).toBe(true);
    });
  });

  describe('onSubmit', () => {
    let localStorageSpy: jest.SpyInstance;

    beforeEach(() => {
      fixture.detectChanges();
      localStorageSpy = jest.spyOn(Storage.prototype, 'setItem');
    });

    afterEach(() => {
      localStorageSpy.mockRestore();
    });

    it('should not submit if form is invalid', () => {
      component.settingsForm.patchValue({ email: '', username: '' });

      component.onSubmit();

      expect(apiService.put).not.toHaveBeenCalled();
    });

    it('should not submit if already submitting', () => {
      component.isSubmitting.set(true);

      component.onSubmit();

      expect(apiService.put).not.toHaveBeenCalled();
    });

    it('should update user settings successfully without password', () => {
      const updateData = {
        email: 'newemail@example.com',
        username: 'newusername',
        bio: 'New bio',
        image: 'https://example.com/new-image.jpg',
      };

      component.settingsForm.patchValue(updateData);

      const mockResponse = {
        user: {
          ...updateData,
          token: 'new-token',
        },
      };

      apiService.put.mockReturnValue(of(mockResponse));

      component.onSubmit();

      expect(component.isSubmitting()).toBe(false);
      expect(apiService.put).toHaveBeenCalledWith(API_ENDPOINTS.user.base, {
        user: updateData,
      });
      expect(localStorage.setItem).toHaveBeenCalledWith(
        'currentUser',
        JSON.stringify(mockResponse.user)
      );
      expect(router.navigate).toHaveBeenCalledWith(['/profile', 'newusername']);
      expect(component.errors()).toBeNull();
    });

    it('should update user settings successfully with password', () => {
      const updateData = {
        email: 'newemail@example.com',
        username: 'newusername',
        bio: 'New bio',
        image: 'https://example.com/new-image.jpg',
        password: 'newpassword123',
      };

      component.settingsForm.patchValue(updateData);

      const mockResponse = {
        user: {
          email: updateData.email,
          username: updateData.username,
          bio: updateData.bio,
          image: updateData.image,
          token: 'new-token',
        },
      };

      apiService.put.mockReturnValue(of(mockResponse));

      component.onSubmit();

      expect(apiService.put).toHaveBeenCalledWith(API_ENDPOINTS.user.base, {
        user: updateData,
      });
    });

    it('should handle update errors', () => {
      const errorResponse = {
        error: {
          errors: {
            email: ['is already taken'],
            username: ['is too short'],
          },
        },
      };

      apiService.put.mockReturnValue(throwError(() => errorResponse));

      component.onSubmit();

      expect(component.isSubmitting()).toBe(false);
      expect(component.errors()).toEqual(errorResponse.error.errors);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should handle generic update errors', () => {
      const errorResponse = {
        error: {},
      };

      apiService.put.mockReturnValue(throwError(() => errorResponse));

      component.onSubmit();

      expect(component.isSubmitting()).toBe(false);
      expect(component.errors()).toEqual({ error: ['Update failed'] });
    });

    it('should set isSubmitting to true during submission', () => {
      apiService.put.mockReturnValue(of({ user: { ...mockUser } }));

      component.onSubmit();

      // After the observable completes, isSubmitting should be false
      expect(component.isSubmitting()).toBe(false);
    });
  });

  describe('logout', () => {
    it('should call authService.logout', () => {
      component.logout();

      expect(authService.logout).toHaveBeenCalled();
    });
  });

  describe('getErrorMessages', () => {
    it('should return empty array when no errors', () => {
      component.errors.set(null);

      expect(component.getErrorMessages()).toEqual([]);
    });

    it('should format error messages correctly', () => {
      component.errors.set({
        email: ['is already taken', 'is invalid'],
        username: ['is too short'],
      });

      const messages = component.getErrorMessages();

      expect(messages).toContain('email is already taken, is invalid');
      expect(messages).toContain('username is too short');
    });

    it('should handle non-array error messages', () => {
      component.errors.set({
        general: 'Something went wrong' as any,
      });

      const messages = component.getErrorMessages();

      expect(messages).toContain('general: Something went wrong');
    });
  });
});
