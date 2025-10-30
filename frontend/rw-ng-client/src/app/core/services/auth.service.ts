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

interface JwtPayload {
  exp?: number;
  [key: string]: any;
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
    const user = storedUser ? JSON.parse(storedUser) : null;

    // Check if stored token is expired and clear it if so
    if (user && this.isTokenExpired(user.token)) {
      localStorage.removeItem('currentUser');
      this.currentUserSubject = new BehaviorSubject<User | null>(null);
    } else {
      this.currentUserSubject = new BehaviorSubject<User | null>(user);
    }

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

  /**
   * Decodes a JWT token and returns its payload
   */
  private decodeToken(token: string): JwtPayload | null {
    try {
      const base64Url = token.split('.')[1];
      if (!base64Url) return null;

      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );

      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('Error decoding token:', error);
      return null;
    }
  }

  /**
   * Checks if a JWT token is expired
   */
  private isTokenExpired(token: string): boolean {
    const payload = this.decodeToken(token);
    if (!payload || !payload.exp) {
      // If we can't decode or there's no expiration, consider it invalid
      return true;
    }

    // JWT exp is in seconds, Date.now() is in milliseconds
    const expirationDate = payload.exp * 1000;
    return Date.now() >= expirationDate;
  }
}
