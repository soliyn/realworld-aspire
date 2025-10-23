import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface RegisterErrors {
  [key: string]: string[];
}

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  errors = signal<RegisterErrors | null>(null);
  isSubmitting = signal(false);

  registerForm = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  onSubmit(): void {
    if (this.registerForm.invalid || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errors.set(null);

    const { username, email, password } = this.registerForm.getRawValue();

    this.authService.register(username, email, password).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigate(['/']);
      },
      error: (error) => {
        this.isSubmitting.set(false);
        this.errors.set(error.error?.errors || { error: ['Registration failed'] });
      },
    });
  }

  getErrorMessages(): string[] {
    const errorObj = this.errors();
    if (!errorObj) return [];

    return Object.entries(errorObj).map(([key, messages]) => {
      if (Array.isArray(messages)) {
        return `${key} ${messages.join(', ')}`;
      }
      return `${key}: ${messages}`;
    });
  }
}
