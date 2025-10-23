import { Routes } from '@angular/router';
import { Home } from './features/home/home';
import { Login } from './features/login/login';
import { Register } from './features/register/register';
import { Profile } from './features/profile/profile';
import { Settings } from './features/settings/settings';
import { authGuard } from './core/services/auth-guard';

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
];
