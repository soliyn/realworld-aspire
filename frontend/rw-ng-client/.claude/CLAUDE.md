# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an Angular 20 frontend application for the RealWorld demo (Conduit clone). It follows modern Angular patterns with standalone components, signals-based state management, and strict TypeScript configuration.

## Development Commands

```bash
# Start development server (runs on http://localhost:4200)
npm start
# or
ng serve

# Build for production
npm run build
# or
ng build

# Run tests with Karma
npm test
# or
ng test

# Watch mode for development builds
npm run watch

# Generate new components, services, etc.
ng generate component component-name
ng generate service service-name
ng generate --help  # See all available schematics
```

## Project Architecture

### Directory Structure

- `src/app/pages/` - Page-level components (e.g., home, settings, new-article)
- `src/app/core/` - Core functionality shared across the app
  - `core/services/` - Singleton services (e.g., ApiService)
  - `core/constants/` - Application constants (e.g., API_ENDPOINTS)
- `src/environments/` - Environment-specific configuration

### Key Architectural Patterns

**Component Naming:** Components use class names without the "Component" suffix (e.g., `Home` instead of `HomeComponent`). This applies to all components in the codebase.

**File Organization:** Each component has its own folder with `.ts`, `.html`, `.scss`, and `.spec.ts` files. Templates and styles are external files, not inline.

**API Communication:** All HTTP requests go through the centralized `ApiService` located in `src/app/core/services/api.service.ts`. This service provides typed methods (`get`, `post`, `put`, `delete`, `patch`) and automatically prepends the base URL from environment config.

**API Endpoints:** Endpoint paths are defined as constants in `src/app/core/constants/api-endpoints.ts` using the `API_ENDPOINTS` object. Use these constants instead of hardcoded strings. The structure supports both static paths and functions for dynamic parameters (e.g., `API_ENDPOINTS.articles.bySlug(slug)`).

**Environment Configuration:** The app uses environment files (`src/environments/environment.ts` for production, `src/environments/environment.development.ts` for development). During development builds, the dev environment file replaces the production one via file replacement configured in `angular.json:56-61`.

**Routing:** Routes are defined in `src/app/app.routes.ts`. The app uses Angular's standalone component routing without NgModules.

**Application Bootstrap:** The app is bootstrapped in `src/main.ts` and configured via `src/app/app.config.ts`, which sets up providers for routing, HttpClient, zone change detection, and error listeners.

## TypeScript Best Practices

- Use strict type checking (enabled in tsconfig.json)
- Prefer type inference when the type is obvious
- Avoid the `any` type; use `unknown` when type is uncertain

## Angular Best Practices

- Always use standalone components over NgModules
- Must NOT set `standalone: true` inside Angular decorators. It's the default.
- Use signals for state management
- Implement lazy loading for feature routes
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead
- Use `NgOptimizedImage` for all static images.
  - `NgOptimizedImage` does not work for inline base64 images.

## Components

- Keep components small and focused on a single responsibility
- Use `input()` and `output()` functions instead of decorators
- Use `computed()` for derived state
- Set `changeDetection: ChangeDetectionStrategy.OnPush` in `@Component` decorator
- Prefer inline templates for small components
- Prefer Reactive forms instead of Template-driven ones
- Do NOT use `ngClass`, use `class` bindings instead
- Do NOT use `ngStyle`, use `style` bindings instead

## State Management

- Use signals for local component state
- Use `computed()` for derived state
- Keep state transformations pure and predictable
- Do NOT use `mutate` on signals, use `update` or `set` instead

## Templates

- Keep templates simple and avoid complex logic
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`
- Use the async pipe to handle observables

## Services

- Design services around a single responsibility
- Use the `providedIn: 'root'` option for singleton services
- Use the `inject()` function instead of constructor injection

## Code Formatting

The project uses Prettier with these settings:
- Print width: 100 characters
- Single quotes for strings
- Angular parser for HTML files

Formatting is configured in `package.json` under the `prettier` field.
