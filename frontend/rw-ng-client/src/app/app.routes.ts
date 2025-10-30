import { Routes } from '@angular/router';
import { Home } from './features/home/home';
import { Login } from './features/login/login';
import { Register } from './features/register/register';
import { Profile } from './features/profile/profile';
import { Settings } from './features/settings/settings';
import { EditArticle } from './features/articles/edit-article/edit-article';
import { authGuard } from './core/services/auth-guard';
import { ViewArticle } from './features/articles/view-article/view-article';

export const routes: Routes = [
  {
    path: '',
    component: Home,
  },
  {
    path: 'login',
    component: Login,
  },
  {
    path: 'register',
    component: Register,
  },
  {
    path: 'profile/:username',
    component: Profile,
  },
  {
    path: 'settings',
    component: Settings,
    canActivate: [authGuard],
  },
  {
    path: 'editor',
    component: EditArticle,
    canActivate: [authGuard],
  },
  {
    path: 'editor/:slug',
    component: EditArticle,
    canActivate: [authGuard],
  },
  {
    path: 'article/:slug',
    component: ViewArticle,
  },
];
