# 13 — Senior Interview Q&A (50 Practical Questions)

> A grab-bag of the questions that actually come up for a **10-year experienced Angular developer**. Every answer is the kind a senior would give: trade-offs, examples, and gotchas — not textbook one-liners.

---

## Architecture & Project Setup

### Q1. How would you structure a large Angular monorepo?
**A.** Nx workspace with multiple `apps/` (host + micro-frontends) and `libs/` split by **scope** (`shared/ui`, `shared/data-access`, `feature-checkout`, `domain-orders`). Enforce import boundaries via Nx tags. Standalone components by default. One `tsconfig.base.json` with path aliases. Shared design tokens in a `theme` lib. CI: affected commands (`nx affected:test`) so PRs only test what changed.

### Q2. Standalone components vs NgModules — what would you recommend?
**A.** Standalone everywhere on new projects (default since v17). Smaller, tree-shakable, easier lazy loading per route. Migrate existing modules incrementally via `ng generate @angular/core:standalone-migration`. Keep `NgModule` only for legacy code you don't touch.

### Q3. When do you reach for NgRx?
**A.** When state is **shared across many features**, has **derived computations** consumed widely, needs **time-travel/audit**, or requires **complex async coordination** (cancellable, retry, debounce). For everything else: a signal-based service. Lately I prefer **NgRx SignalStore** — gets you 80% of NgRx with 20% of the boilerplate.

### Q4. How do you handle micro-frontends in Angular?
**A.** Two options:
1. **Module Federation** (Webpack/Vite) — runtime composition, multiple repos.
2. **Single repo + lazy routes** — simpler if teams accept shared deploys.
For Module Federation: share `@angular/core`, `@angular/common`, `rxjs` as singletons; version compatibility matters. Use `provideRouter` + `loadComponent` in the host pointing at remote bundles.

---

## Components & Change Detection

### Q5. What triggers change detection in an OnPush component?
**A.** Five things: (1) `@Input` reference changes, (2) DOM event from this component or its children, (3) `async` pipe emits, (4) signal read in template changes, (5) `markForCheck()`/`detectChanges()` called.

### Q6. Why is `Default` CD strategy a perf killer in big apps?
**A.** Any zone-patched async event anywhere (setTimeout, scroll, mouse move, HTTP) triggers full top-down CD. Even unaffected components do a dirty-check. With OnPush, untouched subtrees are skipped — work scales with what actually changed.

### Q7. Signals vs `BehaviorSubject` — when use which?
**A.** Signals for **synchronous, derived UI state** with auto-CD. Subjects for **async streams** (websockets, multi-emitter events, complex RxJS pipelines). Bridge with `toSignal` / `toObservable` at the boundary.

### Q8. How do you avoid memory leaks?
**A.** `async` pipe in templates. `takeUntilDestroyed()` in code. Don't store subscriptions in singletons that never die. Unsubscribe `Renderer2.listen` returns. Use `WeakMap` for caches keyed by object. Set `setInterval` in `ngOnInit`, clear in `ngOnDestroy`.

### Q9. `viewChild` vs `contentChild`?
**A.** `viewChild` queries the component's own template (`<app-foo #f>`). `contentChild` queries what's projected into the component via `<ng-content>` (so a parent's template, embedded as a slot).

### Q10. How do smart vs presentational components differ in your code?
**A.** Smart: inject services, fetch data, hold state, OnPush, route-aware. Presentational: only `@Input/@Output`, no DI of services, trivially OnPush, trivially testable, can live in Storybook. Smart maps events to commands; dumb just renders + emits.

---

## Templates, Directives, Pipes

### Q11. Why is calling methods in templates bad?
**A.** They run every CD cycle — performance trap. Move to `computed` signal, pure pipe, or `getter` evaluated once. For lists, derive once in component, bind the derived value.

### Q12. Pure vs impure pipe?
**A.** Pure (default) re-runs only when input reference changes — cached, fast. Impure runs every CD cycle — dangerous in large lists. Prefer pure; do filtering/sorting in component logic with signals.

### Q13. What is `*ngFor`'s `trackBy` (or `track` in @for) and why mandatory?
**A.** A stable identity function — lets Angular reuse DOM nodes across list changes. Without it, Angular destroys & re-creates rows, breaking focus/animations and tanking perf for large lists.

### Q14. Implement a `*appPermission` directive.
```ts
@Directive({ selector: '[appPermission]', standalone: true })
export class PermissionDirective {
  private has = false;
  private tpl = inject(TemplateRef);
  private vc  = inject(ViewContainerRef);
  private auth = inject(AuthService);
  @Input() set appPermission(p: string) {
    const ok = this.auth.has(p);
    if (ok && !this.has) { this.vc.createEmbeddedView(this.tpl); this.has = true; }
    else if (!ok && this.has) { this.vc.clear(); this.has = false; }
  }
}
```
Then `<button *appPermission="'orders.write'">Save</button>`.

### Q15. Why use `Renderer2` over `nativeElement.style`?
**A.** SSR/Web-Worker safety — `Renderer2` is platform-agnostic. Direct DOM access blows up on the server. Also future-proof for Shadow DOM and other render targets.

---

## Forms

### Q16. Template-driven vs Reactive — which and why?
**A.** Reactive Forms 95% of the time. Strongly typed, testable, supports async validators cleanly, scales to dynamic forms via `FormArray`. Template-driven only for trivial cases.

### Q17. How to validate that two fields match?
**A.** Group-level `ValidatorFn` returning `{ mismatch: true }` if `controls.a.value !== controls.b.value`. Display the error from `form.errors`, not from the individual control.

### Q18. How to throttle an async validator?
**A.** Set `updateOn: 'blur'` on the control. Inside the validator, use `switchMap` to cancel previous requests. Cache results in a `Map<string, boolean>` keyed by value to avoid re-asking.

### Q19. How to test a Reactive Form?
**A.** Instantiate the component via `TestBed`, `patchValue` to simulate input, assert `form.valid`, dispatch `submit` event, verify outputs/calls. No real DOM needed for pure logic tests.

### Q20. Implement a custom input that integrates with `formControlName`.
**A.** Implement `ControlValueAccessor` (4 methods: `writeValue`, `registerOnChange`, `registerOnTouched`, `setDisabledState`) and register via `NG_VALUE_ACCESSOR` multi-provider with `forwardRef`. See [07_Forms](07_Forms.md#12-reusable-controlvalueaccessor-custom-input).

---

## RxJS & HttpClient

### Q21. `switchMap` vs `mergeMap` vs `concatMap` vs `exhaustMap`?
**A.** Switch = cancel previous (typeahead). Merge = run in parallel (independent fire-and-forget). Concat = queue, preserve order (save sequence). Exhaust = ignore new while busy (save button double-click guard).

### Q22. How to deduplicate concurrent HTTP calls?
**A.** Wrap once with `shareReplay({ bufferSize: 1, refCount: false })` in the service. Subsequent subscribers reuse the cached emission. For keyed caching, a `Map<key, Observable<T>>` works.

### Q23. Cold vs hot observable?
**A.** Cold creates a new producer per subscriber (HTTP). Hot multicasts a shared producer (WebSocket, Subject). `shareReplay` converts cold to hot with replay.

### Q24. Implement a retry-with-exponential-backoff on 5xx.
```ts
this.http.get(url).pipe(
  retry({
    count: 3,
    delay: (err, attempt) => err.status >= 500
      ? timer(200 * 2 ** attempt)
      : throwError(() => err)
  })
);
```

### Q25. How does a functional interceptor work?
**A.** A function `(req, next) => Observable<HttpEvent>` registered via `withInterceptors([...])`. Chain order matters; each calls `next(req)` to pass forward. Responses come back through them in reverse. Used for auth tokens, error normalization, retry, logging.

### Q26. How to do automatic JWT refresh on 401?
**A.** In the error interceptor: catch 401 → call `auth.refresh()` (shared with `shareReplay(1)` so concurrent 401s don't stampede) → retry original request with new token. Mark refresh request to skip the interceptor and avoid recursion.

### Q27. `forkJoin` vs `combineLatest`?
**A.** `forkJoin` waits for **all to complete**, emits once with the array. Use for parallel one-shot calls. `combineLatest` emits **every time any source emits** (after each has emitted once). Use for derived view models from multiple streams.

### Q28. When does `takeUntilDestroyed()` need a `DestroyRef`?
**A.** Outside the constructor / field initializer (which run in injection context). Pass `inject(DestroyRef)` explicitly: `.pipe(takeUntilDestroyed(this.destroyRef))`.

---

## Routing

### Q29. `canActivate` vs `canMatch`?
**A.** `canMatch` decides if the route is even considered — when false the router moves on, and the lazy chunk isn't downloaded. `canActivate` runs after a route is chosen, deciding whether to enter. Use `canMatch` for role-based routing/feature flags, `canActivate` for auth.

### Q30. How does lazy loading reduce initial bundle?
**A.** `loadComponent` / `loadChildren` use dynamic `import()`. Webpack/esbuild creates separate chunks. The router fetches the chunk on first nav. Initial JS only contains the entry app + shared deps.

### Q31. How to preload lazy chunks?
**A.** `withPreloading(PreloadAllModules)` or a custom strategy that checks `route.data['preload']` (e.g., during idle/post-load).

### Q32. Implement an "unsaved changes" prompt.
**A.** `canDeactivate` guard calling `component.canLeave()` (an interface every editable component implements). Component returns `Observable<boolean>` showing a confirm dialog.

### Q33. What does `withComponentInputBinding()` do?
**A.** Auto-binds matching route params, query params, and resolver keys to component `@Input`/`input()` properties — eliminates `ActivatedRoute.paramMap.subscribe` boilerplate.

---

## DI & Services

### Q34. Hierarchical injector — how does Angular resolve a token?
**A.** Walks from the requesting element injector up through parent components, then to the module/environment injector, then to root. First match wins. `@Optional()`, `@SkipSelf()`, `@Self()`, `@Host()` modify this.

### Q35. Difference between `providedIn: 'root'` and `appConfig.providers`?
**A.** Functionally similar today (both root-scoped). `providedIn: 'root'` is **tree-shakable** — dropped if unused. `appConfig.providers` is always included.

### Q36. What is `APP_INITIALIZER`?
**A.** A multi-provider for async work that must finish before the app bootstraps. Used for loading runtime config, restoring auth, feature flags. Modern: `provideAppInitializer(() => inject(X).load())`.

### Q37. What does `inject()` give you over constructor injection?
**A.** Works in standalone functions (guards/resolvers/interceptors), allows field initializers, enables cleaner code. Both compile to the same DI lookup. Constructor injection still required when extending classes with their own DI needs.

### Q38. Cyclic DI — how to fix?
**A.** Extract shared logic into a third service, use events/streams instead of direct injection, or break the cycle with `forwardRef` and lazy resolution. Most cycles signal a design issue — refactor.

---

## State & Performance

### Q39. When do you choose between component signals and a store?
**A.** Component signals when only the component (and maybe its children) cares. Store when the same data is rendered in multiple unrelated places, derived in complex ways, or has its own lifecycle (cache, optimistic UI, undo/redo).

### Q40. Implement optimistic UI.
**A.** Snapshot previous state, apply change locally, fire API, on error restore snapshot + show toast. Identity by client-generated UUID until server confirms.

### Q41. How would you cut Time-to-Interactive on a slow app?
**A.** Lazy routes + preloading on idle; tree-shake heavy libs (replace moment/lodash); split vendor bundle; `NgOptimizedImage`; defer 3rd-party scripts; OnPush + signals; CDN with HTTP/2; SSR + hydration if LCP requires.

### Q42. Why is mutation evil with OnPush?
**A.** OnPush only marks dirty on input reference change or event/signal in the subtree. Mutating `items.push(x)` keeps the same reference — input check sees no change — DOM doesn't update. Replace with `items = [...items, x]` or use signals.

### Q43. How do you handle 100k-row tables?
**A.** Server pagination + filtering. CDK Virtual Scroll (or ag-Grid) renders only visible rows. `track` by stable ID. OnPush components. Avoid expensive expressions in row templates. Lazy-load the column set.

---

## Testing & Quality

### Q44. Karma vs Jest in 2026?
**A.** Karma is legacy. Use **Jest** (faster, jsdom, watch mode) or **Vitest** if on Vite. Angular 20+ ships `@angular/build:jest` natively.

### Q45. Playwright vs Cypress for E2E?
**A.** **Playwright** in 2026: cross-browser, parallel sharding, native trace viewer, iframe support. Cypress still good DX but slower & Chromium-leaning.

### Q46. How do you write resilient (non-flaky) E2E tests?
**A.** Query by role/label, not CSS classes (use Testing Library or `getByRole`). Wait on state/visibility, never on time. Mock external APIs (Playwright `page.route`). Enable trace on retry. Run on a deterministic seed.

### Q47. How do you test a route guard?
**A.** Use `TestBed.runInInjectionContext` to call the functional guard directly, mock the services it injects, assert the returned UrlTree/boolean.

---

## Security

### Q48. What XSS protections does Angular give you?
**A.** Built-in contextual auto-escaping — `{{ }}` and property bindings escape. `[innerHTML]` is sanitized via `DomSanitizer`. Bypass only with `DomSanitizer.bypassSecurityTrust*` for trusted content. Use `Content-Security-Policy` headers. Avoid `eval` and `Function()`.

### Q49. How do you secure JWTs in the browser?
**A.** Best: **HttpOnly Secure SameSite cookies** set by backend — JS can't read them. If you must store in JS, use `sessionStorage` (cleared on tab close) over `localStorage`; never put refresh tokens client-side. Always include CSRF protection on cookie-based auth.

### Q50. How to handle Entra ID (Azure AD) auth?
**A.** **MSAL Angular** (`@azure/msal-angular`) with redirect or popup flow, configured for OIDC + PKCE. App registration in Entra with SPA platform + redirect URI. Guard routes with `MsalGuard`. Acquire tokens silently via `MsalInterceptor` for backend APIs. For multi-tenant: dynamic authority. Rotate redirect URIs per environment via runtime config.

---

## Bonus: Mini Code Reviews to Practice

### Spot the Bug 1
```ts
@Component({ template: `{{ user.name }}`, changeDetection: ChangeDetectionStrategy.OnPush })
export class UserCardComponent {
  @Input() user!: User;
  ngOnInit() {
    setTimeout(() => this.user.name = 'Updated', 1000);
  }
}
```
**Fix**: Mutating `user.name` doesn't change the input reference; OnPush won't update. Either emit a new object up, use `cdr.markForCheck()`, or model `user` as a signal.

### Spot the Bug 2
```ts
ngOnInit() {
  this.svc.list().subscribe(x => this.users = x);
}
```
**Fix**: No unsubscribe; OnPush won't see the change without `markForCheck`. Use `async` pipe or `toSignal(this.svc.list(), { initialValue: [] })`.

### Spot the Bug 3
```html
<input *ngIf="user" [(ngModel)]="user.name" />
<input *ngFor="let p of phones" [(ngModel)]="p" />
```
**Fix**: Two structural directives on one element — illegal. Wrap the `*ngIf` in `<ng-container>`. And `*ngFor` with `[(ngModel)]` on a primitive doesn't propagate back to the array; use `formArray` + `formControlName="$index"`.

### Spot the Bug 4
```ts
@Injectable({ providedIn: 'root' })
export class DataService {
  data$ = this.http.get('/api/data');
}
```
**Fix**: Each subscriber re-fires the HTTP. Cache: `data$ = this.http.get('/api/data').pipe(shareReplay({ bufferSize: 1, refCount: false }));`

### Spot the Bug 5
```ts
const route = { path: '', redirectTo: 'home' };
```
**Fix**: Needs `pathMatch: 'full'` — without it, the redirect fires on every URL starting with empty (which is all URLs).

---

## How to Approach the Interview Itself

1. **Lead with trade-offs**, not absolutes. "It depends — for X I'd do A, for Y I'd do B."
2. **Mention performance and testability** when designing — interviewers listen for these as senior signals.
3. **Use real war stories**. "Last year we hit this with NgRx Effects and solved it by …" — concrete > theoretical.
4. **Know the modern stack**: signals, standalone, `inject()`, functional guards/interceptors, control flow `@if/@for`, SSR + hydration, zoneless preview.
5. **Be opinionated**. "I'd default to Jest + Playwright + OnPush + Static Web Apps." Then defend with trade-offs if challenged.
6. **Code review thinking**: in any answer, point out what could go wrong, how to test it, and how to roll back.

---

## Mental Model

> **Senior Angular = trade-off fluency. Know the modern defaults (standalone, signals, OnPush, functional APIs), the upgrade paths (NgModule → standalone, BehaviorSubject → signal, classic NgRx → SignalStore, Karma → Jest, Cypress → Playwright), and the operational concerns (perf budgets, CDN caching, runtime config, SSR/hydration). Always answer with a default + why + when you'd switch.**
