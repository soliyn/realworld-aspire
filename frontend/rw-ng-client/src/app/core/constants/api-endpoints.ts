export const API_ENDPOINTS = {
  users: {
    base: '/users',
    login: '/users/login',
  },
  user: {
    base: '/user',
  },
  profiles: {
    base: '/profiles',
    byUsername: (username: string) => `/profiles/${username}`,
    follow: (username: string) => `/profiles/${username}/follow`,
  },
  articles: {
    base: '/articles',
    feed: '/articles/feed',
    bySlug: (slug: string) => `/articles/${slug}`,
    comments: (slug: string) => `/articles/${slug}/comments`,
    commentById: (slug: string, id: number) => `/articles/${slug}/comments/${id}`,
    favorite: (slug: string) => `/articles/${slug}/favorite`,
  },
  tags: {
    base: '/tags',
  },
} as const;
