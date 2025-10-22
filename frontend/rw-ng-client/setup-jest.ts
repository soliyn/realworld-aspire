import { setupZoneTestEnv } from 'jest-preset-angular/setup-env/zone';

setupZoneTestEnv();

Object.defineProperty(window, 'CSS', { value: null });
Object.defineProperty(document, 'doctype', { value: '' });
