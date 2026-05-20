# 11 — Testing (Unit, Component, E2E)

> **One-liner**: Use **Jest** (or Karma+Jasmine) + `TestBed` + `HttpTestingController` for unit/component, **Cypress** or **Playwright** for E2E. Senior expectation: write tests that don't break on cosmetic changes and run fast.

---

## 1. The Testing Pyramid in Angular

```
              /\
             /E2\          Cypress / Playwright — slow, full app
            /----\
           /Comp.\         TestBed component tests — medium
          /-------\
         /Unit ts. \       Pure TS / service tests — fast, many
        /----------\
```

Aim ratio: **70/20/10** (unit / component / e2e).

---

## 2. Tooling Options

| | Karma + Jasmine | Jest | Vitest | Cypress | Playwright |
|---|---|---|---|---|---|
| Type | Unit/component runner | Unit runner | Unit (Vite) | E2E + comp | E2E |
| Default in `ng new`? | Yes (still) | Optional | Possible | No | No |
| Speed | Slow (real browser) | **Fast** (jsdom) | Fastest | – | – |
| Setup | Built-in | `jest-preset-angular` or `@angular/build:jest` (v20) | `@analogjs/vite-plugin-angular` | `ng add @cypress/schematic` | `npm i -D @playwright/test` |

Modern recommendation (2026): **Jest** for unit/component, **Playwright** for E2E. Karma is in maintenance mode.

---

## 3. Switch to Jest (Angular 17+)

```powershell
ng add @angular/build@latest                   # for builders v20+
npm i -D jest @types/jest jest-preset-angular
```

`angular.json`:
```jsonc
"test": {
  "builder": "@angular/build:jest",
  "options": {
    "tsConfig": "tsconfig.spec.json",
    "polyfills": ["zone.js", "zone.js/testing"]
  }
}
```

Or use `jest-preset-angular` for full control:
```js
// jest.config.js
module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEach: ['<rootDir>/setup-jest.ts']
};
```

Run:
```powershell
npm test
npm test -- --watch
npm test -- --coverage
```

---

## 4. Pure TS Unit Test

```ts
// money.ts
export const fmt = (n: number) => '$' + n.toFixed(2);

// money.spec.ts
import { fmt } from './money';

describe('fmt', () => {
  it('formats currency', () => {
    expect(fmt(1.5)).toBe('$1.50');
  });
});
```

No `TestBed` needed for pure functions — fastest path.

---

## 5. Service Test with `TestBed`

```ts
TestBed.configureTestingModule({
  providers: [
    provideHttpClient(),
    provideHttpClientTesting(),
    UserService
  ]
});

const svc  = TestBed.inject(UserService);
const http = TestBed.inject(HttpTestingController);

it('lists users', () => {
  let result: User[] = [];
  svc.list().subscribe(r => result = r);

  const req = http.expectOne('/api/users');
  expect(req.request.method).toBe('GET');
  req.flush([{ id: 1, name: 'Ada' }]);

  expect(result).toEqual([{ id: 1, name: 'Ada' }]);
  http.verify();
});
```

`provideHttpClientTesting()` replaces the real HTTP backend with a mock you control.

---

## 6. Component Test

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

describe('UserCardComponent', () => {
  let fixture: ComponentFixture<UserCardComponent>;
  let cmp: UserCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserCardComponent]           // standalone
    }).compileComponents();

    fixture = TestBed.createComponent(UserCardComponent);
    cmp = fixture.componentInstance;
    fixture.componentRef.setInput('user', { id: 1, name: 'Ada' });
    fixture.detectChanges();
  });

  it('renders name', () => {
    const h = fixture.debugElement.query(By.css('h3'));
    expect(h.nativeElement.textContent).toContain('Ada');
  });

  it('emits select on click', () => {
    let picked: number | undefined;
    cmp.select.subscribe(v => picked = v);
    fixture.debugElement.query(By.css('button')).nativeElement.click();
    expect(picked).toBe(1);
  });
});
```

`setInput` (Angular 14+) is the proper way to set inputs on a standalone component test.

---

## 7. Async Testing — `fakeAsync` & `tick`

```ts
it('debounces typeahead', fakeAsync(() => {
  cmp.search('a');
  cmp.search('ab');
  cmp.search('abc');
  tick(300);                                  // advance timers
  expect(cmp.lastQuery).toBe('abc');
}));
```

Use `fakeAsync` to control time, `flush()` to drain all timers, `flushMicrotasks()` for promises.

For `Promise`-based code without timers: just use `async`/`await`.

---

## 8. Mocking Dependencies

### a) Replace with a stub class
```ts
class FakeAuth { isLoggedIn = () => true; }
TestBed.configureTestingModule({
  providers: [{ provide: AuthService, useClass: FakeAuth }]
});
```

### b) Spies (Jasmine / Jest)
```ts
// Jasmine
const auth = jasmine.createSpyObj<AuthService>('AuthService', ['login']);
auth.login.and.returnValue(of({ token: 'x' }));

// Jest
const auth = { login: jest.fn().mockReturnValue(of({ token: 'x' })) } as any;
```

### c) `provideRouter` test variant
```ts
import { provideRouter } from '@angular/router';
TestBed.configureTestingModule({
  providers: [provideRouter([{ path: 'home', component: HomeComponent }])]
});
```

### d) `ActivatedRoute` mock
```ts
{ provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ id: '1' })) } }
```

---

## 9. Testing Reactive Forms

```ts
it('valid only when both fields filled', () => {
  cmp.form.patchValue({ name: '' });
  expect(cmp.form.valid).toBe(false);
  cmp.form.patchValue({ name: 'A', email: 'a@b.c' });
  expect(cmp.form.valid).toBe(true);
});
```

For custom validators, test them as **plain functions**:
```ts
expect(noBadWords({ value: 'hi'    } as any)).toBeNull();
expect(noBadWords({ value: 'badword' } as any)).toEqual({ badWord: true });
```

---

## 10. Testing Signals

```ts
it('count doubles', () => {
  cmp.count.set(3);
  expect(cmp.double()).toBe(6);
});
```

For `effect`, run in `TestBed.runInInjectionContext` or via `TestBed.flushEffects()` (Angular 19+).

---

## 11. Testing HTTP Interceptors

```ts
TestBed.configureTestingModule({
  providers: [
    provideHttpClient(withInterceptors([authInterceptor])),
    provideHttpClientTesting()
  ]
});

const http = TestBed.inject(HttpClient);
const ctrl = TestBed.inject(HttpTestingController);

http.get('/api/x').subscribe();
const req = ctrl.expectOne('/api/x');
expect(req.request.headers.get('Authorization')).toBe('Bearer fake');
req.flush({});
```

---

## 12. Page Objects / Test Harnesses

For Angular Material (and your own), use **Component Harnesses** — DOM details stay hidden.

```ts
const loader = TestbedHarnessEnvironment.loader(fixture);
const button = await loader.getHarness(MatButtonHarness.with({ text: 'Save' }));
await button.click();
```

For custom components, write your own harness extending `ComponentHarness`. Senior bonus: makes tests immune to template refactors.

---

## 13. Coverage

```powershell
ng test --code-coverage          # Karma
npm test -- --coverage           # Jest
```

Report at `coverage/`. Set thresholds in `karma.conf.js` / `jest.config.js`:
```js
coverageThreshold: { global: { lines: 85, functions: 85, branches: 75 } }
```

---

## 14. E2E with Playwright

```powershell
npm i -D @playwright/test
npx playwright install
```

`e2e/login.spec.ts`:
```ts
import { test, expect } from '@playwright/test';

test('login redirects to dashboard', async ({ page }) => {
  await page.goto('http://localhost:4200/login');
  await page.getByLabel('Email').fill('a@b.c');
  await page.getByLabel('Password').fill('secret');
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page).toHaveURL(/dashboard/);
});
```

`playwright.config.ts` — set `baseURL`, `webServer.command: 'ng serve'`, `webServer.url: 'http://localhost:4200'`, retries, trace, screenshots.

```powershell
npx playwright test            # headless
npx playwright test --ui       # interactive
npx playwright codegen         # record clicks → code
```

---

## 15. E2E with Cypress

```powershell
ng add @cypress/schematic
npx cypress open
```

`cypress/e2e/login.cy.ts`:
```ts
describe('Login', () => {
  it('logs in', () => {
    cy.visit('/login');
    cy.findByLabelText('Email').type('a@b.c');
    cy.findByLabelText('Password').type('secret');
    cy.findByRole('button', { name: /login/i }).click();
    cy.url().should('include', '/dashboard');
  });
});
```

> Senior preference 2026: **Playwright** > Cypress for cross-browser, native iframe support, faster parallelism. Cypress still strong for DX.

---

## 16. CI Pipeline (GitHub Actions)

```yaml
name: ci
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: 20, cache: 'npm' }
      - run: npm ci
      - run: npm run lint
      - run: npm run test -- --watch=false --coverage --browsers=ChromeHeadless
      - run: npm run build
      - run: npx playwright install --with-deps
      - run: npx playwright test
      - uses: actions/upload-artifact@v4
        if: failure()
        with: { name: playwright-trace, path: test-results/ }
```

---

## 17. Common Testing Pitfalls

| Pitfall | Fix |
|---|---|
| `Cannot read property of undefined` in template | Set inputs before `detectChanges` |
| Test passes locally, fails in CI | Headless browser issue / timing — use `fakeAsync` + `tick` |
| Flaky E2E | Use `getByRole`/`getByLabel` queries, wait for state not time, mock APIs |
| Forgetting `http.verify()` | Pending requests leak across tests |
| Real HTTP in unit tests | Always use `provideHttpClientTesting()` |
| Asserting DOM strings | Prefer Testing Library / harnesses |
| Brittle CSS-selector-based tests | Use ARIA roles, data-testids, harnesses |
| Snapshot tests of large DOM | Snapshots become noise; assert key parts only |
| Sleep/wait for time | `tick` (fakeAsync) or expect-based wait |
| Untyped spies | Use `jasmine.SpyObj<T>` / `jest.Mocked<T>` |

---

## 18. Senior Interview Q&A

**Q1. Karma vs Jest vs Vitest — what would you pick today?**
Jest for speed + jsdom + watch mode + huge ecosystem. Vitest if the repo is moving to Vite. Karma is legacy.

**Q2. Cypress vs Playwright?**
Both are great. Playwright wins on cross-browser, parallelism, iframe support, native trace viewer, and CI footprint. Cypress wins on developer ergonomics. Pick one and standardize.

**Q3. How do you test signals + computed?**
`set`/`update` the writable signal, then assert the computed reads. For effects, wrap in `TestBed.runInInjectionContext` or use `flushEffects` (v19+).

**Q4. How to test async code without flakiness?**
`fakeAsync` + `tick` for timers; `async`/`await` for promises; never `setTimeout(...,0)` to "wait". For HTTP, use `HttpTestingController.flush`.

**Q5. How would you test a route guard?**
Call the functional guard directly inside `TestBed.runInInjectionContext`:
```ts
TestBed.runInInjectionContext(() =>
  authGuard({} as any, { url: '/x' } as any)
);
```

**Q6. How to keep tests fast on a large app?**
Jest with parallel workers, no real browser; mock external services; use harnesses; split CI into shards (`--shard 1/4`).

**Q7. What is `provideHttpClientTesting()`?**
Replaces the real HTTP backend with an in-memory mock that lets you `expectOne(...)`, `flush()`, and `verify()`.

**Q8. How do you avoid brittle tests?**
Query by user-facing semantics (label, role) not CSS classes. Use harnesses for Material. Don't assert on implementation details (private state).

**Q9. Test pyramid for an Angular app?**
Many unit (pure functions, services). Some component (rendered HTML + interactions). Few E2E (critical user journeys: login, checkout, search).

**Q10. How would you debug a flaky Playwright test in CI?**
Enable trace on retry (`use: { trace: 'retain-on-failure' }`), look at the trace viewer (`playwright show-trace …`), and fix the underlying race (wait on state, not time).

---

## 19. Cheat Sheet

```ts
// TestBed
await TestBed.configureTestingModule({
  imports: [MyStandaloneComponent],
  providers: [provideHttpClient(), provideHttpClientTesting()]
}).compileComponents();

// Component
fixture.componentRef.setInput('user', { ... });
fixture.detectChanges();

// Async
fakeAsync(() => { … tick(300); });

// HTTP
const r = http.expectOne('/api/x'); r.flush(body); http.verify();
```

```powershell
npm test
npx playwright test --ui
```

---

## 20. Mental Model

> **Tests should describe behavior, not implementation. Unit-test pure logic with Jest. Use `TestBed` + harnesses + Testing-Library queries for component tests. Mock HTTP, time, and routes. Pick Playwright for E2E and run trace on failures. The right pyramid keeps CI fast and PRs trustworthy.**
