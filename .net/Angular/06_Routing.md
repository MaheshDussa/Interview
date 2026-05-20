# 06 — Routing

> **One-liner**: Angular Router maps URLs to component trees, lazy-loads features, guards access, resolves data before activation, and animates between views.

---

## 1. Setup (Standalone, Angular 17+)

```ts
// app.routes.ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'home' },
  { path: 'home', loadComponent: () => import('./home/home.component').then(m => m.HomeComponent) },
  { path: 'users', loadChildren: () => import('./users/users.routes').then(m => m.USER_ROUTES) },
  { path: '**', component: NotFoundComponent }
];

// app.config.ts
providers: [
  provideRouter(routes, withComponentInputBinding(), withViewTransitions())
]
```

```html
<!-- app.component.html -->
<nav>
  <a routerLink="/home" routerLinkActive="active">Home</a>
  <a [routerLink]="['/users', userId]">User</a>
</nav>
<router-outlet />
```

Don't forget to import `RouterOutlet`, `RouterLink`, `RouterLinkActive` in standalone components that use them.

---

## 2. Route Configuration Anatomy

```ts
{
  path: 'orders/:id',
  component: OrderDetailComponent,        // or loadComponent
  title: 'Order details',                 // sets <title> automatically
  data: { breadcrumb: 'Order' },          // arbitrary static data
  resolve: { order: orderResolver },      // pre-fetch data
  canMatch: [featureFlagGuard],           // route taken into account at all?
  canActivate: [authGuard],               // gate entry
  canDeactivate: [unsavedGuard],          // gate exit
  canActivateChild: [adminGuard],         // gate child routes
  providers: [OrdersStore],               // feature-scoped DI
  children: [ ... ],                      // nested routes
  outlet: 'aux'                           // secondary <router-outlet name="aux">
}
```

---

## 3. Navigation APIs

```ts
// In template
<a [routerLink]="['/users', id]" [queryParams]="{ tab: 'orders' }" fragment="top">…</a>

// In code
constructor(private router: Router) {}
this.router.navigate(['/users', id], {
  queryParams: { page: 2 },
  queryParamsHandling: 'merge',     // merge | preserve
  state: { from: 'list' },
  replaceUrl: true
});
this.router.navigateByUrl('/users');
```

Active link:
```html
<a routerLink="/home" routerLinkActive="active"
   [routerLinkActiveOptions]="{ exact: true }">Home</a>
```

---

## 4. Reading Route Params

Three observables on `ActivatedRoute`:

```ts
constructor(private route: ActivatedRoute) {}
ngOnInit() {
  this.route.paramMap.subscribe(p => this.id = p.get('id'));
  this.route.queryParamMap.subscribe(q => this.page = +q.get('page')!);
  this.route.data.subscribe(d => this.order = d['order']);
}
```

**Modern (Angular 16+) — `withComponentInputBinding()`**: route params are auto-bound to `@Input` / `input()` of the component.

```ts
// routes
provideRouter(routes, withComponentInputBinding())

// component
@Component({ /* ... */ })
export class OrderDetailComponent {
  id = input<string>();       // matches :id
  tab = input<string>();      // matches ?tab=
  order = input<Order>();     // matches resolve key
}
```

No more manual `subscribe` to route params.

---

## 5. Guards (Functional, Angular 14.2+)

```ts
// canActivate
export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() || router.parseUrl(`/login?next=${state.url}`);
};

// canMatch — runs BEFORE the route is even considered (preferable for lazy chunks)
export const featureFlagGuard: CanMatchFn = (route) => {
  return inject(Flags).enabled(route.data?.['flag']);
};

// canDeactivate
export interface CanLeave { canLeave(): boolean | Promise<boolean>; }
export const unsavedGuard: CanDeactivateFn<CanLeave> =
  (cmp) => cmp.canLeave();
```

Use cases:
- `canMatch` — feature flags, role-based route routing (cleaner than `canActivate` because the route is hidden entirely + the lazy chunk isn't downloaded)
- `canActivate` — auth
- `canActivateChild` — protect entire feature subtree
- `canDeactivate` — "unsaved changes?" prompt

> Senior point: prefer **functional guards** + `inject()` over old class-based `Injectable()` guards. They tree-shake better and are simpler to test.

---

## 6. Resolvers — Pre-Fetch Data

```ts
export const orderResolver: ResolveFn<Order> = (route) => {
  return inject(OrderService).getById(route.paramMap.get('id')!);
};

{ path: 'orders/:id', component: OrderDetailComponent, resolve: { order: orderResolver } }
```

Component reads `route.data['order']` or, with input binding, `order = input<Order>()`.

**When to use**: small, fast fetches required before view renders.
**When NOT to use**: large fetches that would block navigation — fetch in `ngOnInit` and show a loader instead.

---

## 7. Lazy Loading

**Lazy component** (Angular 14+):
```ts
{ path: 'about', loadComponent: () => import('./about.component').then(m => m.AboutComponent) }
```

**Lazy feature (children)**:
```ts
// users.routes.ts
export const USER_ROUTES: Routes = [
  { path: '', component: UserListComponent },
  { path: ':id', component: UserDetailComponent }
];

// app.routes.ts
{ path: 'users', loadChildren: () => import('./users/users.routes').then(m => m.USER_ROUTES) }
```

Each lazy chunk becomes its own JS file — keeps initial bundle small.

**Preloading strategies**:
```ts
provideRouter(routes, withPreloading(PreloadAllModules));
// or custom:
@Injectable({ providedIn: 'root' })
export class SelectivePreload implements PreloadingStrategy {
  preload(route: Route, fn: () => Observable<unknown>) {
    return route.data?.['preload'] ? fn() : of(null);
  }
}
provideRouter(routes, withPreloading(SelectivePreload));
```

Mark with `data: { preload: true }` on routes that should load in idle.

---

## 8. Nested Routes (Child Outlets)

```ts
{ path: 'admin', component: AdminShellComponent,
  children: [
    { path: '', component: AdminDashboardComponent },
    { path: 'users', component: AdminUsersComponent },
    { path: 'roles', component: AdminRolesComponent }
  ]
}
```

`AdminShellComponent.html`:
```html
<aside><nav> ... </nav></aside>
<main><router-outlet /></main>
```

---

## 9. Secondary (Auxiliary) Outlets

```html
<router-outlet name="aside" />
```
```ts
{ path: 'help', component: HelpPanel, outlet: 'aside' }
```
Navigate:
```ts
this.router.navigate([{ outlets: { aside: ['help'] } }]);
this.router.navigate([{ outlets: { aside: null } }]); // close
```

URL: `/users(aside:help)`.

Useful for chat sidebars, contextual help panels.

---

## 10. Route Title

```ts
{ path: 'users/:id', component: UserDetailComponent, title: 'User detail' }

// Dynamic
{ path: 'users/:id', component: UserDetailComponent, title: userTitleResolver }

export const userTitleResolver: ResolveFn<string> = (route) =>
  inject(UserService).getName(route.paramMap.get('id')!).pipe(map(n => `User – ${n}`));
```

Set the app title strategy:
```ts
provideRouter(routes, withRouterConfig({})) // default uses TitleStrategy
```

---

## 11. URL Matching Strategies

- **Default (prefix match)** — most routes
- **`pathMatch: 'full'`** — required on empty-path redirects (`{ path: '', redirectTo: 'home', pathMatch: 'full' }`)
- **Wildcard `**`** — catch-all, last route

```ts
{ path: '', pathMatch: 'full', redirectTo: 'home' },
{ path: '**', component: NotFoundComponent }
```

---

## 12. Animations Between Routes

```html
<div [@routeAnimations]="getState(outlet)">
  <router-outlet #outlet="outlet" />
</div>
```

```ts
trigger('routeAnimations', [
  transition('* => *', [
    style({ opacity: 0 }), animate('200ms', style({ opacity: 1 }))
  ])
])
getState(o: RouterOutlet) { return o?.activatedRouteData?.['animation']; }
```

Or use **`withViewTransitions()`** for native browser View Transitions API (smoother, less code).

---

## 13. Router Events

```ts
this.router.events.pipe(
  filter(e => e instanceof NavigationStart)
).subscribe(...);
```

Useful events: `NavigationStart`, `NavigationEnd`, `NavigationCancel`, `NavigationError`, `RouteConfigLoadStart` (chunk download), `Scroll`.

Use for: page-view analytics, top-of-page progress bar, scroll restoration.

```ts
provideRouter(routes,
  withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' })
);
```

---

## 14. Common Pitfalls

| Pitfall | Fix |
|---|---|
| `routerLink` doesn't navigate | Component didn't import `RouterLink` |
| Empty-path redirect ignored | Add `pathMatch: 'full'` |
| Guard injects something out of context | Use `inject()` inside the function body |
| Route params don't update on same-component navigation | Subscribe to `paramMap` or use input binding |
| Lazy bundle fetched on app start | Avoid eager imports; check `loadChildren` is a dynamic `import()` |
| Resolver blocks navigation forever | Resolvers should be fast; throw or return early |
| Back button doesn't restore scroll | Use `withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })` |
| 404 catches everything but `/` | Order routes; wildcard goes **last** |

---

## 15. Senior Interview Q&A

**Q1. `canActivate` vs `canMatch`?**
`canMatch` runs before the router considers a route — if false, router moves on to the next match (lazy chunk isn't fetched). `canActivate` runs after the route is chosen and decides whether to actually enter. Use `canMatch` for feature flags / role routing, `canActivate` for auth checks.

**Q2. How does lazy loading work?**
`loadChildren` / `loadComponent` return dynamic `import()`s. The bundler creates separate chunks. The router fetches the chunk on first navigation to that route.

**Q3. Why prefer functional guards?**
They're standalone functions using `inject()` — easier to test, no DI registration overhead, better tree-shaking.

**Q4. Difference between `Resolver` and `ngOnInit` fetch?**
Resolver pre-fetches before the route activates; the view doesn't render until it resolves. `ngOnInit` lets the view render with a loader. Use Resolver for required, fast data; `ngOnInit` otherwise.

**Q5. How to share route params with child routes?**
Use `paramsInheritanceStrategy: 'always'` or read from `route.parent.paramMap`.

**Q6. How to keep query params on navigation?**
`queryParamsHandling: 'preserve' | 'merge'`.

**Q7. Implement "unsaved changes" prompt.**
`canDeactivate` guard calling a method (`canLeave`) on the component; component returns boolean / Observable.

**Q8. What's `withComponentInputBinding()`?**
Auto-binds route params, query params, and resolved data to component `@Input`s of the same name — eliminates manual `ActivatedRoute` subscriptions.

**Q9. How to preload some chunks?**
Provide a custom `PreloadingStrategy` that checks `route.data['preload']` and call via `withPreloading(...)`.

**Q10. How does the router serialize URLs?**
Uses `UrlSerializer`. Aux outlets become `/main(aux:something)`. Matrix params like `;k=v` are allowed but rarely used today.

---

## 16. Cheat Sheet

```powershell
ng g c users/user-list
ng g guard auth                # CanActivateFn
ng g resolver user
```

```ts
provideRouter(
  routes,
  withComponentInputBinding(),
  withPreloading(PreloadAllModules),
  withInMemoryScrolling({ scrollPositionRestoration: 'enabled' }),
  withViewTransitions()
)
```

```ts
{ path: '', pathMatch: 'full', redirectTo: 'home' }
{ path: '**', component: NotFoundComponent }
{ path: 'x', canMatch:[flag], canActivate:[auth], canDeactivate:[unsaved],
  resolve: { data: r }, providers: [Store],
  loadComponent: () => import('./x.component').then(m => m.X) }
```

---

## 17. Mental Model

> **Router = URL → component tree. Use standalone routes, `loadComponent`/`loadChildren` everywhere, functional guards with `inject()`, resolvers for required data, `withComponentInputBinding()` to skip subscribe boilerplate. Each lazy feature can scope its own services via route `providers`.**
