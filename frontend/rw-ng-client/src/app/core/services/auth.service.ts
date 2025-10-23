import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Router } from '@angular/router';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/api-endpoints';

export interface User {
  email: string;
  token: string;
  username: string;
  bio?: string;
  image?: string;
}

interface LoginResponse {
  user: User;
}

interface RegisterResponse {
  user: User;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiService = inject(ApiService);
  private router = inject(Router);

  private currentUserSubject: BehaviorSubject<User | null>;
  public currentUser$: Observable<User | null>;

  constructor() {
    // Initialize with user from localStorage if exists
    const storedUser = localStorage.getItem('currentUser');
    this.currentUserSubject = new BehaviorSubject<User | null>(
      storedUser ? JSON.parse(storedUser) : null
    );
    this.currentUser$ = this.currentUserSubject.asObservable();
  }

  public get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  login(email: string, password: string): Observable<User> {
    return this.apiService
      .post<LoginResponse>(API_ENDPOINTS.users.login, { user: { email, password } })
      .pipe(
        map((response) => {
          const user = response.user;
          // Store user in localStorage
          localStorage.setItem('currentUser', JSON.stringify(user));
          // Update the current user subject
          this.currentUserSubject.next(user);
          return user;
        })
      );
  }

  register(username: string, email: string, password: string): Observable<User> {
    return this.apiService
      .post<RegisterResponse>(API_ENDPOINTS.users.base, { user: { username, email, password } })
      .pipe(
        map((response) => {
          const user = response.user;
          // Store user in localStorage
          localStorage.setItem('currentUser', JSON.stringify(user));
          // Update the current user subject
          this.currentUserSubject.next(user);
          return user;
        })
      );
  }

  logout(): void {
    // Remove user from localStorage
    localStorage.removeItem('currentUser');
    // Update the current user subject
    this.currentUserSubject.next(null);
    // Navigate to login page
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return !!this.currentUserValue?.token;
  }

  getToken(): string | null {
    return this.currentUserValue?.token || null;
  }
}
