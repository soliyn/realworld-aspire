# RealWorld Conduit - Angular 20 Frontend

This project is an Angular 20 implementation of the RealWorld demo application (Conduit clone). It demonstrates modern Angular patterns including standalone components, signals-based state management, and strict TypeScript configuration.

Generated using [Angular CLI](https://github.com/angular/angular-cli) version 20.3.6.

## Table of Contents

- [Development Commands](#development-commands)
- [Project Architecture](#project-architecture)
- [Non-Standard Dependencies](#non-standard-dependencies)
- [Code Standards & Best Practices](#code-standards--best-practices)
- [Additional Resources](#additional-resources)

## Development Commands

```bash
# Start development server (http://localhost:4200)
npm start
# or
ng serve

# Build for production
npm run build

# Run unit tests with Jest
npm test

# Run tests in watch mode
npm run test:watch

# Generate test coverage report
npm run test:coverage

# Debug tests
npm run test:debug

# Watch mode for development builds
npm run watch

# Generate new components, services, etc.
ng generate component component-name
ng generate service service-name
ng generate --help  # See all available schematics
```

## Project Architecture

### Directory Structure

```
src/app/
├── core/                          # Core singleton services and shared models
│   ├── constants/
│   │   └── api-endpoints.ts       # Centralized API endpoint definitions
│   ├── models/
│   │   ├── article.model.ts       # Article interfaces and types
│   │   ├── comment.model.ts       # Comment interfaces
│   │   └── profile.model.ts       # Profile interfaces
│   └── services/
│       ├── api.service.ts         # Generic HTTP wrapper service
│       ├── auth.service.ts        # Authentication and user management (BehaviorSubject-based)
│       ├── auth.interceptor.ts    # Adds JWT token to requests (functional interceptor)
│       ├── auth-guard.ts          # Route guard for protected routes (functional guard)
│       ├── error.interceptor.ts   # Handles 401/403 errors globally (functional interceptor)
│       └── feed-state.service.ts  # Global feed state management (signals)
│
├── features/                      # Feature modules/components
│   ├── articles/                  # Article-related components
│   │   ├── article-list/          # List of articles with pagination
│   │   ├── article-list-item/     # Single article preview card (presentational)
│   │   ├── edit-article/          # Create/edit article form (reactive forms)
│   │   ├── paginator/             # Reusable pagination component
│   │   ├── view-article/          # Article detail page with comments (markdown rendering)
│   │   └── articles.service.ts    # Article CRUD operations
│   ├── home/                      # Landing page with article feeds
│   ├── login/                     # Login page (reactive forms)
│   ├── not-found/                 # 404 page
│   ├── profile/                   # User profile with articles/favorites tabs
│   │   └── profile.service.ts     # Follow/unfollow operations
│   ├── register/                  # Registration page (reactive forms)
│   ├── settings/                  # User settings page
│   └── tag-list/                  # Popular tags sidebar
│       └── tags.service.ts        # Fetch tags from API
│
├── app.ts                         # Root component
├── app.html                       # Root template with nav and footer
├── app.config.ts                  # Application providers configuration
└── app.routes.ts                  # Route definitions
```

### Core Services

#### ApiService

Centralized HTTP wrapper that provides typed methods (`get<T>()`, `post<T>()`, `put<T>()`, `delete<T>()`, `patch<T>()`) and automatically prepends the base URL from environment configuration.

#### AuthService

Manages authentication state using RxJS `BehaviorSubject` for reactive user state. Features:

- JWT token management with localStorage persistence
- Token expiration checking and auto-logout
- Login, register, and logout operations
- Observable `currentUser$` stream for reactive updates

#### FeedStateService

Signal-based global state management for feed type selection:

- Feed types: 'global', 'your-feed', 'tag'
- Computed signals for feed type checks
- Shared state across Home and ArticleList components

#### Feature Services

- **ArticlesService**: All article and comment CRUD operations, favorites, feed retrieval
- **ProfileService**: User profile retrieval and follow/unfollow operations
- **TagsService**: Fetch popular tags from API

### Key Architectural Patterns

#### Component Architecture

- **Standalone Components**: All components use the standalone API (no NgModules)
- **OnPush Change Detection**: All components use `ChangeDetectionStrategy.OnPush` for optimal performance
- **Naming Convention**: Class names without "Component" suffix (e.g., `Home` instead of `HomeComponent`)
- **File Organization**: Separate `.ts`, `.html`, `.scss`, and `.spec.ts` files for each component

#### State Management

**Primary Pattern: Signals**

```typescript
// Writable signals for local state
const isLoading = signal(false);
const errors = signal<Error | null>(null);

// Computed signals for derived state
const hasErrors = computed(() => errors() !== null);
```

**Integration with RxJS**

```typescript
// Convert observables to signals
const data = toSignal(
  this.service.getData().pipe(
    map((response) => response.data),
    startWith(initialValue)
  ),
  { initialValue }
);

// Convert signals to observables
const params$ = toObservable(this.paramsSignal);
```

**Complex State Management** (e.g., Profile component)

- Uses `toSignal()` + `toObservable()` + `switchMap()` for declarative reactive loading
- RxJS Subjects for imperative actions (follow, favorite)
- Maps for tracking optimistic updates
- Multiple computed signals for derived state

#### Form Handling

- **Always Reactive Forms**: Uses `FormBuilder.nonNullable.group()` for type-safe forms
- **Validation**: Built-in validators (`Validators.required`, `Validators.email`, `Validators.minLength`)
- **State Management**: Signals for `isSubmitting`, `errors`
- **Error Display**: Helper methods to format and display API errors

#### API Communication

1. **Endpoint Constants**: All API paths defined in `core/constants/api-endpoints.ts`

   ```typescript
   API_ENDPOINTS.articles.bySlug(slug); // Function for dynamic params
   API_ENDPOINTS.users.login; // Static paths
   ```

2. **Service Pattern**: All HTTP calls go through `ApiService`

   ```typescript
   this.apiService.get<ResponseType>(API_ENDPOINTS.path, params);
   ```

3. **Response Handling**: Observable pipelines with error handling and state updates

#### Template Patterns

- **Native Control Flow**: Uses `@if`, `@for`, `@switch` (NOT `*ngIf`, `*ngFor`, `*ngSwitch`)
- **Class Bindings**: `[class.active]="condition()"` (NOT `ngClass`)
- **Signal Values**: Invoked as functions `{{ signal() }}`
- **Async Pipe**: Used for observables in templates

#### Dependency Injection

Uses the `inject()` function instead of constructor injection:

```typescript
private authService = inject(AuthService);
```

#### Routing

Routes defined in [app.routes.ts](src/app/app.routes.ts):

- `/` - Home page
- `/login`, `/register` - Authentication pages
- `/profile/:username` - User profile
- `/settings` - User settings (protected)
- `/editor`, `/editor/:slug` - Create/edit articles (protected)
- `/article/:slug` - Article detail page
- `/**` - 404 page

Protected routes use the functional `authGuard` that redirects to login with returnUrl query parameter.

#### HTTP Interceptors

- **authInterceptor**: Functional interceptor that adds JWT token to all requests
- **errorInterceptor**: Functional interceptor for global error handling (auto-logout on 401/403)

### Environment Configuration

- **Production**: `src/environments/environment.ts` - API URL: `https://api.realworld.show/api`
- **Development**: `src/environments/environment.development.ts` - API URL: `http://localhost:5386/api`
- File replacement configured in [angular.json](angular.json) (lines 52-56)

### TypeScript Configuration

Strict mode enabled with:

- `strict: true`
- `noImplicitOverride: true`
- `noPropertyAccessFromIndexSignature: true`
- `noImplicitReturns: true`
- `noFallthroughCasesInSwitch: true`
- `strictTemplates: true` (Angular templates)
- Target: ES2022

## Non-Standard Dependencies

These packages are not part of a standard Angular application:

### Runtime Dependencies

#### marked (^16.4.1)

**Purpose**: Markdown parser and compiler
**Usage**: Converts article body (markdown) to HTML in the [view-article](src/app/features/articles/view-article/view-article.ts) component
**Implementation**: `marked.parse(article.body, { async: false })` in computed signal
**Why needed**: RealWorld articles are stored as markdown and need to be rendered as HTML

### Development Dependencies

#### @testing-library/angular (^18.1.0)

**Purpose**: Component testing library
**Usage**: Provides user-centric testing utilities for Angular components
**Why needed**: Alternative to Angular TestBed with better testing patterns (query utilities, user event simulation)
**Related packages**:

- `@testing-library/dom` (^10.0.0) - DOM testing utilities
- `@testing-library/jest-dom` (^6.9.1) - Custom Jest matchers for DOM
- `@testing-library/user-event` (^14.6.1) - User interaction simulation

#### jest (^30.2.0)

**Purpose**: Testing framework
**Usage**: Replaces Karma/Jasmine for unit testing
**Why needed**: Faster test execution, better TypeScript support, modern testing experience
**Configuration**: [jest.config.js](jest.config.js)
**Related packages**:

- `jest-preset-angular` (^15.0.3) - Jest preset for Angular
- `jest-environment-jsdom` (^30.2.0) - DOM environment for Jest
- `@types/jest` (^30.0.0) - TypeScript types for Jest
- `ts-jest` (^29.4.5) - TypeScript preprocessor for Jest
- `jsdom` (^27.0.1) - JavaScript implementation of web standards

#### run-script-os (^1.1.6)

**Purpose**: Cross-platform npm scripts
**Usage**: Runs different scripts based on OS (Windows vs Unix-like)
**Why needed**: Handles environment variable syntax differences (`%PORT%` on Windows, `$PORT` on Unix)
**Implementation**: See [package.json](package.json) lines 6-8

#### cross-env (^10.1.0)

**Purpose**: Cross-platform environment variable setting
**Usage**: Not currently used in package.json scripts but available for future cross-platform environment variable management
**Why needed**: Ensures consistent environment variable handling across Windows, Mac, and Linux

## Code Standards & Best Practices

### Component Guidelines

1. **Keep components small** and focused on a single responsibility
2. **Use `input()` and `output()` functions** instead of `@Input`/`@Output` decorators
3. **Use `computed()` for derived state** to ensure automatic dependency tracking
4. **Always set `changeDetection: ChangeDetectionStrategy.OnPush`** in `@Component` decorator
5. **Do NOT set `standalone: true`** - it's the default in Angular 20
6. **Avoid `@HostBinding` and `@HostListener`** - use `host` object in decorator instead
7. **Do NOT use `ngClass` or `ngStyle`** - use direct class/style bindings instead
8. **Use `NgOptimizedImage`** for all static images (not for inline base64)

### State Management Guidelines

1. **Use signals for local component state** (writable signals with `signal()`)
2. **Use `computed()` for derived state** to automatically track dependencies
3. **Keep state transformations pure** and predictable
4. **Do NOT use `mutate` on signals** - use `update` or `set` instead
5. **Use `toSignal()` for declarative observable-to-signal conversion**
6. **Use `toObservable()` when you need to convert signals back to observables**

### Template Guidelines

1. **Keep templates simple** and avoid complex logic
2. **Use native control flow** (`@if`, `@for`, `@switch`) instead of structural directives
3. **Use the async pipe** to handle observables in templates
4. **Invoke signal values as functions** in templates: `{{ mySignal() }}`

### Service Guidelines

1. **Design services around a single responsibility**
2. **Use `providedIn: 'root'`** for singleton services
3. **Use the `inject()` function** instead of constructor injection

### Code Formatting

The project uses Prettier with these settings:

- Print width: 100 characters
- Single quotes for strings
- Angular parser for HTML files
- Configuration in [package.json](package.json) (lines 16-27)

### Testing Guidelines

1. **Use Jest** instead of Karma/Jasmine
2. **Use Angular Testing Library** for component testing
3. **Test file naming**: `*.spec.ts` files alongside implementation files
4. **Coverage**: Available via `npm run test:coverage`

## Additional Resources

- [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli)
- [Angular Signals Documentation](https://angular.dev/guide/signals)
- [RealWorld API Specification](https://realworld-docs.netlify.app/specifications/backend/endpoints/)
- [Angular Testing Library](https://testing-library.com/docs/angular-testing-library/intro/)
- [Jest Documentation](https://jestjs.io/docs/getting-started)
- [Marked Documentation](https://marked.js.org/)
