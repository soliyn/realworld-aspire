import { Profile } from './profile.model';

export interface Article {
  slug: string;
  title: string;
  description: string;
  body: string;
  tagList: string[];
  createdAt: string;
  updatedAt: string;
  favorited: boolean;
  favoritesCount: number;
  author: Profile;
}

export interface ArticleResponse {
  article: Article;
}

export interface ArticlesResponse {
  articles: Article[];
  articlesCount: number;
}

export interface NewArticle {
  title: string;
  description: string;
  body: string;
  tagList?: string[];
}

export interface UpdateArticle {
  title?: string;
  description?: string;
  body?: string;
  tagList?: string[];
}
