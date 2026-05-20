# 02 — Angular Basics

> **One-liner**: Angular = TypeScript + Components + Templates + Dependency Injection + Reactive data + RxJS. Everything else is a layer on top.

---

## 1. The Big Picture

```
HTML template  ◄──►  Component (TS class)  ◄──►  Service (data / logic)
       ▲                                                ▲
       └── Directives/Pipes        Router/Forms         └── HttpClient/Signals/RxJS
```

A page = a tree of **components**. Each component owns its template, styles, and a slice of state. Components talk to **services** via constructor injection. Services use **HttpClient** + **RxJS** to call APIs.

---

## 2. Anatomy of a Component

```ts
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-counter',                   // <app-counter/>
  standalone: true,
  imports: [CommonModule],
  templateUrl: './counter.component.html',
  styleUrl: './counter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CounterComponent {
  count = signal(0);
  inc() { this.count.update(c => c + 1); }
}
```

Three files per component (default):
- `*.component.ts` — class + decorator
- `*.component.html` — template
- `*.component.scss/css` — scoped styles

`@Component` is a decorator that attaches metadata.

---

## 3. Templates & Interpolation

```html
<h1>Hello, {{ name }}</h1>
<p>{{ 1 + 2 }}</p>
<p>{{ user?.address?.city ?? 'N/A' }}</p>
```

- `{{ expr }}` — string interpolation (evaluates a JS-like expression).
- No statements (`if`, `for`) allowed inline — use template syntax instead.
- Safe navigation `?.` to avoid NRE.

---

## 4. Data Binding — Four Forms

| Form | Syntax | Direction |
|---|---|---|
| Interpolation | `{{ value }}` | Class → DOM |
| Property binding | `[disabled]="isBusy"` | Class → DOM |
| Event binding | `(click)="save()"` | DOM → Class |
| Two-way (banana-in-a-box) | `[(ngModel)]="name"` | both |

```html
<input [value]="name" (input)="name = $any($event.target).value" />
<input [(ngModel)]="name" />   <!-- needs FormsModule -->
<button [disabled]="loading" (click)="save()">Save</button>
<img [src]="user.avatar" [attr.alt]="user.name" />
```

---

## 5. Built-in Control Flow (Angular 17+)

Replaces `*ngIf`, `*ngFor`, `*ngSwitch`. Use this — it's the **modern syntax**.

```html
@if (user(); as u) {
  <p>Hello {{ u.name }}</p>
} @else if (loading()) {
  <p>Loading…</p>
} @else {
  <p>Anonymous</p>
}

@for (item of items(); track item.id) {
  <li>{{ item.name }}</li>
} @empty {
  <li>No items</li>
}

@switch (role) {
  @case ('admin') { <admin-panel/> }
  @case ('user')  { <user-panel/> }
  @default        { <guest-panel/> }
}
```

`track` is **mandatory** in `@for` — pick a stable identifier (id) for diffing performance.

Legacy (still works):
```html
<p *ngIf="loading; else done">Loading…</p>
<ng-template #done>Done</ng-template>
<li *ngFor="let i of items; trackBy: trackById">{{ i.name }}</li>
```

---

## 6. Common Pipes

```html
{{ price | currency:'USD':'symbol':'1.2-2' }}
{{ now   | date:'medium':'UTC' }}
{{ name  | uppercase }}
{{ obj   | json }}
{{ user$ | async }}              <!-- subscribes & unsubscribes for you -->
{{ list  | slice:0:5 }}
{{ count | i18nPlural:msgs }}
{{ count | number:'1.0-2' }}
{{ percent | percent:'1.0-1' }}
```

`async` pipe is your best friend with Observables/Promises — never call `subscribe` in components if `| async` works.

---

## 7. Inputs & Outputs (parent ↔ child)

Modern `input()` / `output()` (Angular 17.1+):

```ts
import { Component, input, output } from '@angular/core';

@Component({ selector: 'app-user-card', standalone: true,
  template: `
    <h3>{{ user().name }}</h3>
    <button (click)="select.emit(user().id)">Pick</button>` })
export class UserCardComponent {
  user   = input.required<User>();
  badge  = input<string>('default');
  select = output<number>();
}
```

Parent:
```html
<app-user-card [user]="u" badge="vip" (select)="onPick($event)"/>
```

Legacy decorators still work:
```ts
@Input() user!: User;
@Output() select = new EventEmitter<number>();
```

---

## 8. Component Selectors

Three forms:
```ts
selector: 'app-foo'         // <app-foo></app-foo>
selector: '[appFoo]'        // <div appFoo></div>  — attribute
selector: '.app-foo'        // <div class="app-foo"></div>  — class
```

Prefix all selectors (`app-`, or company prefix `acme-`) to avoid collisions.

---

## 9. Styling

- Each component has **scoped styles** by default (`ViewEncapsulation.Emulated` via attribute selectors).
- Global styles live in `src/styles.scss`.
- `:host` styles the component element itself.
- `:host-context(.dark)` reacts to ancestor class.
- `::ng-deep` (deprecated) pierces encapsulation — avoid; use CSS variables instead.

```scss
:host { display: block; padding: 1rem; }
:host(.active) { border: 2px solid blue; }
.title { font-weight: 600; }
```

CSS variables work natively:
```scss
:host { --gap: 1rem; }
.row { gap: var(--gap); }
```

---

## 10. Project File Walkthrough

```
src/
├── app/
│   ├── core/                # singleton services, guards, interceptors
│   ├── shared/              # reusable components, directives, pipes
│   ├── features/
│   │   ├── users/
│   │   │   ├── pages/
│   │   │   ├── components/
│   │   │   └── users.routes.ts
│   │   └── orders/
│   ├── app.component.*
│   ├── app.config.ts
│   └── app.routes.ts
├── assets/
├── environments/
└── styles.scss
```

Convention: **feature folders**, lazy-loaded routes per feature, `core` for app-wide services, `shared` for UI primitives.

---

## 11. Bootstrapping (`main.ts` + `app.config.ts`)

```ts
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    { provide: APP_BASE_HREF, useValue: '/' }
  ]
};

// main.ts
bootstrapApplication(AppComponent, appConfig).catch(console.error);
```

---

## 12. TypeScript Essentials Used Constantly

```ts
interface User { id: number; name: string; email?: string; }
type Role = 'admin' | 'user' | 'guest';

function pick<T, K extends keyof T>(obj: T, k: K): T[K] { return obj[k]; }

const arr: ReadonlyArray<User> = [];
const map = new Map<string, User>();
```

Senior expectation: **strict TS** — no `any` leaks, use unions/generics/`Partial`/`Pick`/`Omit`.

---

## 13. The `async` Pipe vs Manual Subscribe

```ts
users$ = this.svc.list();
```
```html
@if (users$ | async; as users) {
  @for (u of users; track u.id) { <li>{{ u.name }}</li> }
}
```

Or with signals (preferred in Angular 18+):
```ts
users = toSignal(this.svc.list(), { initialValue: [] });
```
```html
@for (u of users(); track u.id) { <li>{{ u.name }}</li> }
```

No `OnDestroy` needed — both auto-clean.

---

## 14. Common Mistakes Beginners Make (and seniors catch in PR review)

| Mistake | Fix |
|---|---|
| Calling `subscribe` and never unsubscribing | Use `async` pipe / `toSignal` / `takeUntilDestroyed()` |
| `*ngFor` without `trackBy` / `track` | Always provide |
| Big methods in templates `{{ doExpensiveWork() }}` | Move to memoized signal / pipe — runs every CD cycle |
| Mutating `@Input` objects | Treat inputs as immutable; emit events |
| `any` everywhere | Type properly |
| One God component | Split by responsibility |
| Logic in `ngOnChanges` instead of `computed`/`effect` | Use signals |
| `ngModel` two-way for everything | Reactive forms for non-trivial cases |
| Direct DOM with `document.querySelector` | Use `viewChild` / `@ViewChild` |
| Forgetting `CommonModule` in standalone component using `*ngIf` | Import it (or switch to `@if`) |

---

## 15. Cheat Sheet

```powershell
ng g c users/user-list                # generate component
ng g s data --skip-tests              # service
ng serve --port 4300 --open
ng build --configuration=production
```

```html
{{ value }}                           [prop]="x"   (event)="fn()"   [(ngModel)]="x"
@if / @else / @else if
@for (x of xs; track x.id) {} @empty {}
@switch (v) { @case (1){} @default{} }
| async   | date:'short'   | currency:'USD'
```

---

## 16. Mental Model

> **Component = (class + template + styles) wired through Angular's DI. Templates use `{{ }}`, `[ ]`, `( )`, `[( )]` plus `@if/@for/@switch`. Default to standalone components, signals, and the `async` pipe. Everything else is layered on top.**
