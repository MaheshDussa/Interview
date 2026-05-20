# Angular — End-to-End Senior Interview Track

A 13-file practical study path covering **Angular from scratch to senior mastery** for a 10-year-experienced developer interview. Modern Angular (v17–v20): **standalone components, signals, control flow, functional guards/interceptors, SSR + hydration, zoneless preview**.

Every file follows the same format: ~20 numbered sections, comparison tables, copy-paste-ready code, PowerShell CLI commands, common pitfalls, senior interview Q&A, and a closing **Mental model**.

---

## Files

| # | File | Covers |
|---|------|--------|
| 01 | [01_Setup_And_CLI.md](01_Setup_And_CLI.md) | Node prereqs, `@angular/cli`, `ng new`, full `ng generate` reference, workspace + `angular.json`, environments, `ng update`, package managers, VS Code setup, day-1 troubleshooting |
| 02 | [02_Angular_Basics.md](02_Angular_Basics.md) | Big picture, component anatomy, templates, 4 forms of data binding, `@if`/`@for`/`@switch`, pipes, `input()`/`output()`, selectors, styling, bootstrap with `app.config.ts`, async pipe vs `toSignal` |
| 03 | [03_Components_Deep.md](03_Components_Deep.md) | Lifecycle order, communication patterns, content projection, `viewChild`/`contentChild`, View Encapsulation, OnPush, signals deep dive, dynamic components, host bindings, smart vs dumb |
| 04 | [04_Directives_And_Pipes.md](04_Directives_And_Pipes.md) | Attribute + structural directive authoring, `hostDirectives` composition API, built-in + custom pipes, pure vs impure, `Renderer2`, real-world directives (`*appPermission`, `*appLet`) |
| 05 | [05_Services_And_DI.md](05_Services_And_DI.md) | `@Injectable`, hierarchical DI, provider scopes (`root`/`platform`/`any`/route/component), `useClass`/`useExisting`/`useValue`/`useFactory`, `InjectionToken`, multi-providers, `APP_INITIALIZER` |
| 06 | [06_Routing.md](06_Routing.md) | Standalone routes, `loadComponent`/`loadChildren`, **functional guards** (`canMatch`/`canActivate`/`canDeactivate`), resolvers, **`withComponentInputBinding()`**, preloading strategies, aux outlets, View Transitions |
| 07 | [07_Forms.md](07_Forms.md) | Template-driven vs **Reactive Forms** (typed, `FormBuilder.nonNullable`), validators (sync + async + cross-field), `FormArray`, `ControlValueAccessor` for custom inputs, error display patterns, `updateOn` |
| 08 | [08_HttpClient_And_RxJS.md](08_HttpClient_And_RxJS.md) | `provideHttpClient(withFetch(), withInterceptors([...]))`, typed requests, params/headers/progress, functional interceptors, retry+backoff, `shareReplay` caching, **switch/concat/merge/exhaustMap**, `takeUntilDestroyed` |
| 09 | [09_State_Management.md](09_State_Management.md) | The state ladder (signal → service → SignalStore → NgRx), signal-based store pattern, **NgRx SignalStore**, classic NgRx with Effects + Entity, optimistic UI, URL-as-state |
| 10 | [10_Performance_And_ChangeDetection.md](10_Performance_And_ChangeDetection.md) | Zone.js, OnPush mental model, `markForCheck` vs `detectChanges`, `runOutsideAngular`, lazy load, **bundle budgets + source-map-explorer**, `NgOptimizedImage`, CDK Virtual Scroll, SSR + hydration, zoneless preview |
| 11 | [11_Testing.md](11_Testing.md) | Jest > Karma, `TestBed` + `HttpTestingController`, component tests with `setInput`, `fakeAsync`+`tick`, signal testing, interceptor tests, **Playwright** E2E, harnesses, CI workflow |
| 12 | [12_Build_Deploy.md](12_Build_Deploy.md) | Production build, **bundle budgets**, runtime vs build-time config, **Azure Static Web Apps / App Service / Container Apps / Blob+Front Door**, multi-stage Dockerfile with Nginx, SSR container, pre-render, OIDC CI/CD |
| 13 | [13_Senior_Interview_QA.md](13_Senior_Interview_QA.md) | **50 senior-level Q&As** with trade-off-driven answers + 5 mini code-review "spot the bug" exercises + how-to-approach-the-interview advice |

---

## Suggested Reading Order

1. **Total newcomer** → 01 → 02 → 04 → 03 → 05 → 06 → 07
2. **Already know Angular, target senior** → 03 → 08 → 09 → 10 → 11 → 13
3. **Pure interview prep** → 13, skim 03/08/09/10 Q&A sections
4. **Production hardening** → 10 → 11 → 12

---

## Modern Angular Defaults (2026)

Use these as your "what I'd build today" baseline in interviews:

- **Standalone components everywhere** — no `NgModule`
- **Signals + `computed` + `effect`** for synchronous state
- **`@if` / `@for (track)` / `@switch`** built-in control flow
- **OnPush** as the default change detection (set via schematics)
- **Reactive Forms** (typed, `nonNullable`)
- **Functional guards / interceptors** with `inject()`
- **`withComponentInputBinding()`** on the router
- **`provideHttpClient(withFetch(), withInterceptors([...]))`**
- **`async` pipe** or `toSignal()` in templates — no manual `.subscribe()`
- **`takeUntilDestroyed()`** for the rare code-side subscribe
- **NgRx SignalStore** when you outgrow a signal-based service
- **Standalone routes with `loadComponent` / `loadChildren`**
- **`NgOptimizedImage`** for images
- **Jest + Playwright** for tests
- **Azure Static Web Apps** for SPA hosting, **Container Apps** for SSR
- **Runtime config JSON** so one image promotes through environments
- **Bundle budgets enforced in CI** + source-map-explorer audit
- **Zoneless** when stable (Angular 19/20+) — already preview-able today

---

## CLI Cheat Sheet

```powershell
# Install
npm install -g @angular/cli

# Project
ng new my-app --routing --style=scss --standalone --strict
cd my-app
ng serve --open
ng build --configuration=production
ng test
ng lint

# Generate
ng g c users/user-list
ng g s services/user
ng g d shared/highlight
ng g p pipes/truncate
ng g guard auth
ng g resolver user
ng g interceptor auth
ng g i models/user
ng g lib shared

# Update
ng update
ng update @angular/core @angular/cli

# Add capabilities
ng add @angular/ssr
ng add @angular/material
ng add @ngrx/store
ng add @cypress/schematic

# Build & analyze
ng build --stats-json
npx source-map-explorer dist/my-app/browser/*.js
```

---

## Related Tracks in This Workspace

| Folder | Topic |
|---|---|
| [../AI200/01_Compute/](../AI200/01_Compute/) | App Service, Functions, VMs (.NET cloud compute) |
| [../AI200/02_Containers/](../AI200/02_Containers/) | Docker, ACR — pair with Angular's [12_Build_Deploy.md](12_Build_Deploy.md) |
| [../AI200/04_Security_Identity/](../AI200/04_Security_Identity/) | Entra ID auth — pair with MSAL Angular |
| [../AI200/05_AZ204_Exam/](../AI200/05_AZ204_Exam/) | AZ-204 practice questions |
| [../AI200/08_CSharp_Interview/](../AI200/08_CSharp_Interview/) | C# senior interview deep dive (backend pair) |
| [../Python/](../Python/) | Python end-to-end track |

---

## Senior Interview Survival Kit

1. **Lead with trade-offs**, not absolutes.
2. **Cite the modern default** + when you'd choose the alternative.
3. **Demonstrate measurement** — "I'd profile with Angular DevTools / Chrome / source-map-explorer before optimizing."
4. **Bring a war story** for every major topic.
5. **Show code-review thinking** — what could go wrong, how to test it, how to roll back.

---

## Mental Model

> **Senior Angular fluency = modern defaults (standalone + signals + OnPush + functional APIs) + production discipline (budgets, lazy load, CDN, runtime config, rollback) + measurement (DevTools, Lighthouse, source-map-explorer) + clear trade-offs when picking patterns. Master those four and you can defend any Angular decision in an interview.**
