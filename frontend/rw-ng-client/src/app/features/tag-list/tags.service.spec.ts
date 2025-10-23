import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { TagsService, TagsResponse } from './tags.service';
import { ApiService } from '../../core/services/api.service';
import { API_ENDPOINTS } from '../../core/constants/api-endpoints';

describe('TagsService', () => {
  let service: TagsService;
  let apiService: jest.Mocked<ApiService>;

  beforeEach(() => {
    const apiServiceMock = {
      get: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        TagsService,
        { provide: ApiService, useValue: apiServiceMock },
      ],
    });

    service = TestBed.inject(TagsService);
    apiService = TestBed.inject(ApiService) as jest.Mocked<ApiService>;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getTags', () => {
    it('should call ApiService.get with correct endpoint', () => {
      const mockResponse: TagsResponse = {
        tags: ['angular', 'typescript', 'testing'],
      };

      apiService.get.mockReturnValue(of(mockResponse));

      service.getTags().subscribe();

      expect(apiService.get).toHaveBeenCalledWith(API_ENDPOINTS.tags.base);
      expect(apiService.get).toHaveBeenCalledTimes(1);
    });

    it('should return tags response from API', (done) => {
      const mockResponse: TagsResponse = {
        tags: ['angular', 'typescript', 'testing'],
      };

      apiService.get.mockReturnValue(of(mockResponse));

      service.getTags().subscribe((response) => {
        expect(response).toEqual(mockResponse);
        expect(response.tags).toHaveLength(3);
        expect(response.tags).toContain('angular');
        expect(response.tags).toContain('typescript');
        expect(response.tags).toContain('testing');
        done();
      });
    });

    it('should handle empty tags array', (done) => {
      const mockResponse: TagsResponse = {
        tags: [],
      };

      apiService.get.mockReturnValue(of(mockResponse));

      service.getTags().subscribe((response) => {
        expect(response.tags).toEqual([]);
        expect(response.tags).toHaveLength(0);
        done();
      });
    });
  });
});
