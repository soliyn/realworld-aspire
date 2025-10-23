import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, RouterModule } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { Register } from './register';
import { AuthService } from '../../core/services/auth.service';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;
  let authService: jest.Mocked<AuthService>;
  let router: Router;

  const mockUser = {
    email: 'test@example.com',
    token: 'test-token-123',
    username: 'testuser',
  };

  beforeEach(async () => {
    const authServiceMock = {
      register: jest.fn(),
      login: jest.fn(),
      logout: jest.fn(),
      isAuthenticated: jest.fn(),
      getToken: jest.fn(),
      currentUserValue: null,
      currentUser$: of(null),
    };

    await TestBed.configureTestingModule({
      imports: [Register, RouterModule.forRoot([])],
      providers: [{ provide: AuthService, useValue: authServiceMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;
    router = TestBed.inject(Router);
    jest.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('form initialization', () => {
    it('should initialize with empty username, email and password fields', () => {
      expect(component.registerForm.get('username')?.value).toBe('');
      expect(component.registerForm.get('email')?.value).toBe('');
      expect(component.registerForm.get('password')?.value).toBe('');
    });

    it('should have username field with required and minLength validators', () => {
      const usernameControl = component.registerForm.get('username');

      usernameControl?.setValue('');
      expect(usernameControl?.hasError('required')).toBe(true);

      usernameControl?.setValue('ab');
      expect(usernameControl?.hasError('minlength')).toBe(true);

      usernameControl?.setValue('abc');
      expect(usernameControl?.valid).toBe(true);
    });

    it('should have email field with required and email validators', () => {
      const emailControl = component.registerForm.get('email');

      emailControl?.setValue('');
      expect(emailControl?.hasError('required')).toBe(true);

      emailControl?.setValue('invalid-email');
      expect(emailControl?.hasError('email')).toBe(true);

      emailControl?.setValue('valid@example.com');
      expect(emailControl?.valid).toBe(true);
    });

    it('should have password field with required and minLength validators', () => {
      const passwordControl = component.registerForm.get('password');

      passwordControl?.setValue('');
      expect(passwordControl?.hasError('required')).toBe(true);

      passwordControl?.setValue('12345');
      expect(passwordControl?.hasError('minlength')).toBe(true);

      passwordControl?.setValue('123456');
      expect(passwordControl?.valid).toBe(true);
    });

    it('should be invalid when empty', () => {
      expect(component.registerForm.valid).toBe(false);
    });

    it('should be valid when all fields are correctly filled', () => {
      component.registerForm.patchValue({
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
      });

      expect(component.registerForm.valid).toBe(true);
    });
  });

  describe('signals initialization', () => {
    it('should initialize errors signal as null', () => {
      expect(component.errors()).toBeNull();
    });

    it('should initialize isSubmitting signal as false', () => {
      expect(component.isSubmitting()).toBe(false);
    });
  });

  describe('onSubmit', () => {
    beforeEach(() => {
      component.registerForm.patchValue({
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
      });
    });

    it('should not submit if form is invalid', () => {
      component.registerForm.patchValue({ username: '', email: '', password: '' });

      component.onSubmit();

      expect(authService.register).not.toHaveBeenCalled();
    });

    it('should not submit if already submitting', () => {
      component.isSubmitting.set(true);

      component.onSubmit();

      expect(authService.register).not.toHaveBeenCalled();
    });

    it('should call authService.register with username, email and password', () => {
      authService.register.mockReturnValue(of(mockUser));

      component.onSubmit();

      expect(authService.register).toHaveBeenCalledWith(
        'testuser',
        'test@example.com',
        'password123'
      );
    });

    it('should set isSubmitting to true when submitting', () => {
      let resolveRegister: ((value: typeof mockUser) => void) | undefined;
      const registerPromise = new Promise<typeof mockUser>((resolve) => {
        resolveRegister = resolve;
      });
      authService.register.mockReturnValue(
        new Observable((subscriber) => {
          registerPromise.then((value) => {
            subscriber.next(value);
            subscriber.complete();
          });
        })
      );

      component.onSubmit();

      expect(component.isSubmitting()).toBe(true);

      // Clean up - resolve the promise
      resolveRegister?.(mockUser);
    });

    it('should clear errors when submitting', () => {
      component.errors.set({ error: ['Previous error'] });
      authService.register.mockReturnValue(of(mockUser));

      component.onSubmit();

      expect(component.errors()).toBeNull();
    });

    describe('on successful registration', () => {
      beforeEach(() => {
        authService.register.mockReturnValue(of(mockUser));
      });

      it('should set isSubmitting to false', (done) => {
        component.onSubmit();

        setTimeout(() => {
          expect(component.isSubmitting()).toBe(false);
          done();
        });
      });

      it('should navigate to home page', (done) => {
        component.onSubmit();

        setTimeout(() => {
          expect(router.navigate).toHaveBeenCalledWith(['/']);
          done();
        });
      });

      it('should not set errors', (done) => {
        component.onSubmit();

        setTimeout(() => {
          expect(component.errors()).toBeNull();
          done();
        });
      });
    });

    describe('on failed registration', () => {
      const errorResponse = {
        error: {
          errors: {
            email: ['is already taken'],
          },
        },
      };

      beforeEach(() => {
        authService.register.mockReturnValue(throwError(() => errorResponse));
      });

      it('should set isSubmitting to false', (done) => {
        component.onSubmit();

        setTimeout(() => {
          expect(component.isSubmitting()).toBe(false);
          done();
        });
      });

      it('should not navigate', (done) => {
        component.onSubmit();

        setTimeout(() => {
          expect(router.navigate).not.toHaveBeenCalled();
          done();
        });
      });

      it('should set errors from error response', (done) => {
        component.onSubmit();

        setTimeout(() => {
          expect(component.errors()).toEqual({
            email: ['is already taken'],
          });
          done();
        });
      });

      it('should set default error if error response has no errors', (done) => {
        authService.register.mockReturnValue(throwError(() => ({ error: {} })));

        component.onSubmit();

        setTimeout(() => {
          expect(component.errors()).toEqual({
            error: ['Registration failed'],
          });
          done();
        });
      });
    });
  });

  describe('getErrorMessages', () => {
    it('should return empty array when errors is null', () => {
      component.errors.set(null);

      expect(component.getErrorMessages()).toEqual([]);
    });

    it('should return formatted error messages for single error', () => {
      component.errors.set({
        email: ['is already taken'],
      });

      expect(component.getErrorMessages()).toEqual(['email is already taken']);
    });

    it('should return formatted error messages for multiple errors', () => {
      component.errors.set({
        email: ['is required'],
        username: ['is too short'],
      });

      const messages = component.getErrorMessages();
      expect(messages).toContain('email is required');
      expect(messages).toContain('username is too short');
      expect(messages.length).toBe(2);
    });

    it('should handle multiple messages for a single field', () => {
      component.errors.set({
        email: ['is required', 'must be valid'],
      });

      expect(component.getErrorMessages()).toEqual(['email is required, must be valid']);
    });

    it('should handle non-array error values', () => {
      component.errors.set({
        error: 'Something went wrong' as any,
      });

      expect(component.getErrorMessages()).toEqual(['error: Something went wrong']);
    });
  });

  describe('template rendering', () => {
    it('should render sign up title', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const title = compiled.querySelector('h1');

      expect(title?.textContent).toContain('Sign up');
    });

    it('should render link to login page', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const link = compiled.querySelector('a[routerLink="/login"]');

      expect(link?.textContent).toContain('Have an account?');
    });

    it('should render username input field', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const usernameInput = compiled.querySelector('input[formControlName="username"]');

      expect(usernameInput).toBeTruthy();
      expect(usernameInput?.getAttribute('placeholder')).toBe('Username');
    });

    it('should render email input field', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const emailInput = compiled.querySelector('input[type="email"]');

      expect(emailInput).toBeTruthy();
      expect(emailInput?.getAttribute('placeholder')).toBe('Email');
    });

    it('should render password input field', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const passwordInput = compiled.querySelector('input[type="password"]');

      expect(passwordInput).toBeTruthy();
      expect(passwordInput?.getAttribute('placeholder')).toBe('Password');
    });

    it('should render submit button', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const button = compiled.querySelector('button[type="submit"]');

      expect(button?.textContent?.trim()).toBe('Sign up');
    });

    it('should disable submit button when form is invalid', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const button = compiled.querySelector('button[type="submit"]') as HTMLButtonElement;

      expect(button?.disabled).toBe(true);
    });

    it('should enable submit button when form is valid', () => {
      component.registerForm.patchValue({
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
      });
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const button = compiled.querySelector('button[type="submit"]') as HTMLButtonElement;

      expect(button?.disabled).toBe(false);
    });

    it('should disable submit button when submitting', () => {
      component.registerForm.patchValue({
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
      });
      component.isSubmitting.set(true);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const button = compiled.querySelector('button[type="submit"]') as HTMLButtonElement;

      expect(button?.disabled).toBe(true);
    });

    it('should not display error messages when errors is null', () => {
      component.errors.set(null);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const errorList = compiled.querySelector('.error-messages');

      expect(errorList).toBeFalsy();
    });

    it('should display error messages when errors exist', () => {
      component.errors.set({
        email: ['is already taken'],
      });
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const errorList = compiled.querySelector('.error-messages');
      const errorItems = compiled.querySelectorAll('.error-messages li');

      expect(errorList).toBeTruthy();
      expect(errorItems.length).toBe(1);
      expect(errorItems[0].textContent).toContain('email is already taken');
    });

    it('should update username field value when user types', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const usernameInput = compiled.querySelector(
        'input[formControlName="username"]'
      ) as HTMLInputElement;

      usernameInput.value = 'testuser';
      usernameInput.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.registerForm.get('username')?.value).toBe('testuser');
    });

    it('should update email field value when user types', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const emailInput = compiled.querySelector('input[type="email"]') as HTMLInputElement;

      emailInput.value = 'test@example.com';
      emailInput.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.registerForm.get('email')?.value).toBe('test@example.com');
    });

    it('should update password field value when user types', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const passwordInput = compiled.querySelector('input[type="password"]') as HTMLInputElement;

      passwordInput.value = 'password123';
      passwordInput.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.registerForm.get('password')?.value).toBe('password123');
    });

    it('should call onSubmit when form is submitted', () => {
      const onSubmitSpy = jest.spyOn(component, 'onSubmit');
      component.registerForm.patchValue({
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
      });
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const form = compiled.querySelector('form');
      form?.dispatchEvent(new Event('submit'));

      expect(onSubmitSpy).toHaveBeenCalled();
    });
  });
});
