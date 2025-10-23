import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ProfileService } from './profile.service';
import { ApiService } from '../../core/services/api.service';
import { API_ENDPOINTS } from '../../core/constants/api-endpoints';
import { ProfileResponse } from '../../core/models/profile.model';

describe('ProfileService', () => {
  let service: ProfileService;
  let apiService: jest.Mocked<ApiService>;

  const mockProfile = {
    username: 'testuser',
    bio: 'Test bio',
    image: 'https://example.com/avatar.jpg',
    following: false,
  };

  const mockProfileResponse: ProfileResponse = {
    profile: mockProfile,
  };

  beforeEach(() => {
    const apiServiceMock = {
      get: jest.fn(),
      post: jest.fn(),
      delete: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [ProfileService, { provide: ApiService, useValue: apiServiceMock }],
    });

    service = TestBed.inject(ProfileService);
    apiService = TestBed.inject(ApiService) as jest.Mocked<ApiService>;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getProfile', () => {
    it('should call apiService.get with correct endpoint', () => {
      apiService.get.mockReturnValue(of(mockProfileResponse));

      service.getProfile('testuser').subscribe((response) => {
        expect(response).toEqual(mockProfileResponse);
      });

      expect(apiService.get).toHaveBeenCalledWith(API_ENDPOINTS.profiles.byUsername('testuser'));
    });

    it('should return profile data for different usernames', () => {
      const differentProfile: ProfileResponse = {
        profile: {
          username: 'anotheruser',
          bio: 'Another bio',
          image: 'https://example.com/another.jpg',
          following: true,
        },
      };

      apiService.get.mockReturnValue(of(differentProfile));

      service.getProfile('anotheruser').subscribe((response) => {
        expect(response).toEqual(differentProfile);
        expect(response.profile.username).toBe('anotheruser');
      });

      expect(apiService.get).toHaveBeenCalledWith(API_ENDPOINTS.profiles.byUsername('anotheruser'));
    });
  });

  describe('followUser', () => {
    it('should call apiService.post with correct endpoint and empty body', () => {
      const followedProfileResponse: ProfileResponse = {
        profile: { ...mockProfile, following: true },
      };

      apiService.post.mockReturnValue(of(followedProfileResponse));

      service.followUser('testuser').subscribe((response) => {
        expect(response).toEqual(followedProfileResponse);
        expect(response.profile.following).toBe(true);
      });

      expect(apiService.post).toHaveBeenCalledWith(API_ENDPOINTS.profiles.follow('testuser'), {});
    });

    it('should work with different usernames', () => {
      const followedProfileResponse: ProfileResponse = {
        profile: {
          username: 'anotheruser',
          bio: 'Another bio',
          image: 'https://example.com/another.jpg',
          following: true,
        },
      };

      apiService.post.mockReturnValue(of(followedProfileResponse));

      service.followUser('anotheruser').subscribe((response) => {
        expect(response.profile.username).toBe('anotheruser');
        expect(response.profile.following).toBe(true);
      });

      expect(apiService.post).toHaveBeenCalledWith(
        API_ENDPOINTS.profiles.follow('anotheruser'),
        {}
      );
    });
  });

  describe('unfollowUser', () => {
    it('should call apiService.delete with correct endpoint', () => {
      const unfollowedProfileResponse: ProfileResponse = {
        profile: { ...mockProfile, following: false },
      };

      apiService.delete.mockReturnValue(of(unfollowedProfileResponse));

      service.unfollowUser('testuser').subscribe((response) => {
        expect(response).toEqual(unfollowedProfileResponse);
        expect(response.profile.following).toBe(false);
      });

      expect(apiService.delete).toHaveBeenCalledWith(API_ENDPOINTS.profiles.follow('testuser'));
    });

    it('should work with different usernames', () => {
      const unfollowedProfileResponse: ProfileResponse = {
        profile: {
          username: 'anotheruser',
          bio: 'Another bio',
          image: 'https://example.com/another.jpg',
          following: false,
        },
      };

      apiService.delete.mockReturnValue(of(unfollowedProfileResponse));

      service.unfollowUser('anotheruser').subscribe((response) => {
        expect(response.profile.username).toBe('anotheruser');
        expect(response.profile.following).toBe(false);
      });

      expect(apiService.delete).toHaveBeenCalledWith(
        API_ENDPOINTS.profiles.follow('anotheruser')
      );
    });
  });
});
