import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { API_ENDPOINTS } from '../../core/constants/api-endpoints';

export interface TagsResponse {
  tags: string[];
}

@Injectable({
  providedIn: 'root'
})
export class TagsService {
  private apiService = inject(ApiService);

  getTags(): Observable<TagsResponse> {
    return this.apiService.get<TagsResponse>(API_ENDPOINTS.tags.base);
  }
}
