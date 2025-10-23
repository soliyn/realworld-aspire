import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { API_ENDPOINTS } from '../../core/constants/api-endpoints';
import { ProfileResponse } from '../../core/models/profile.model';

@Injectable({
  providedIn: 'root',
})
export class ProfileService {
  private apiService = inject(ApiService);

  getProfile(username: string): Observable<ProfileResponse> {
    return this.apiService.get<ProfileResponse>(API_ENDPOINTS.profiles.byUsername(username));
  }

  followUser(username: string): Observable<ProfileResponse> {
    return this.apiService.post<ProfileResponse>(API_ENDPOINTS.profiles.follow(username), {});
  }

  unfollowUser(username: string): Observable<ProfileResponse> {
    return this.apiService.delete<ProfileResponse>(API_ENDPOINTS.profiles.follow(username));
  }
}
