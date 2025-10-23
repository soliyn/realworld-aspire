import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ApiService } from '../../core/services/api.service';
import { API_ENDPOINTS } from '../../core/constants/api-endpoints';

interface UpdateUserData {
  email: string;
  username: string;
  bio: string;
  image: string;
  password?: string;
}

interface UpdateUserResponse {
  user: {
    email: string;
    token: string;
    username: string;
    bio?: string;
    image?: string;
  };
}

interface SettingsErrors {
  [key: string]: string[];
}

@Component({
  selector: 'app-settings',
  imports: [ReactiveFormsModule],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Settings implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private apiService = inject(ApiService);
  private router = inject(Router);

  errors = signal<SettingsErrors | null>(null);
  isSubmitting = signal(false);

  settingsForm = this.fb.nonNullable.group({
    image: [''],
    username: ['', [Validators.required]],
    bio: [''],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
  });

  ngOnInit(): void {
    const currentUser = this.authService.currentUserValue;
    if (!currentUser) {
      this.router.navigate(['/login']);
      return;
    }

    // Pre-populate form with current user data
    this.settingsForm.patchValue({
      image: currentUser.image || '',
      username: currentUser.username,
      bio: currentUser.bio || '',
      email: currentUser.email,
    });
  }

  onSubmit(): void {
    if (this.settingsForm.invalid || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errors.set(null);

    const formValue = this.settingsForm.getRawValue();
    const updateData: UpdateUserData = {
      email: formValue.email,
      username: formValue.username,
      bio: formValue.bio,
      image: formValue.image,
    };

    // Only include password if it was provided
    if (formValue.password) {
      updateData.password = formValue.password;
    }

    this.apiService.put<UpdateUserResponse>(API_ENDPOINTS.user.base, { user: updateData }).subscribe({
      next: (response) => {
        this.isSubmitting.set(false);
        // Update the current user in AuthService
        localStorage.setItem('currentUser', JSON.stringify(response.user));
        // Redirect to profile page
        this.router.navigate(['/profile', response.user.username]);
      },
      error: (error) => {
        this.isSubmitting.set(false);
        this.errors.set(error.error?.errors || { error: ['Update failed'] });
      },
    });
  }

  logout(): void {
    this.authService.logout();
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
