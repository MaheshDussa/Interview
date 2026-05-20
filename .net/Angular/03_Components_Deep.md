# 03 — Components Deep Dive

> **One-liner**: Master the **lifecycle**, **parent/child communication**, **content projection**, **view queries**, **change detection**, and you have the senior-level grip on Angular components.

---

## 1. Component Lifecycle Hooks

Run in order on the instance (called by Angular at well-defined times):

| Hook | When | Use for |
|---|---|---|
| `constructor` | New instance | DI grabs only — no work |
| `ngOnChanges(changes)` | Inputs change (before & each update) | React to bound input changes |
| `ngOnInit` | After **first** `ngOnChanges` | Init logic, fetch data |
| `ngDoCheck` | Every CD run | Custom diff — rare, performance trap |
| `ngAfterContentInit` | After projected `<ng-content>` is initialized | Read `contentChild`/`@ContentChild` |
| `ngAfterContentChecked` | After every CD of projected content | Validate projected content |
| `ngAfterViewInit` | After view (own template) is initialized | Read `viewChild`/`@ViewChild`, focus, charts |
| `ngAfterViewChecked` | After every CD of own view | Rarely needed |
| `ngOnDestroy` | Right before component is removed | Cleanup: subscriptions, intervals, listeners |

Modern alternative — **`DestroyRef`** + `takeUntilDestroyed()`:
```ts
constructor(private destroyRef: DestroyRef) {
  this.svc.stream().pipe(takeUntilDestroyed(destroyRef))
    .subscribe(v => this.value = v);
}
```

---

## 2. Component Communication Patterns

### a) Parent → Child: **Inputs**
```ts
@Component({ selector: 'app-child', template: `{{ data() }}` })
export class ChildComponent {
  data = input.required<string>();
}
```
```html
<app-child [data]="parentValue" />
```

### b) Child → Parent: **Outputs**
```ts
saved = output<User>();
this.saved.emit(user);
```
```html
<app-child (saved)="onSaved($event)" />
```

### c) Parent reaches into Child: **viewChild**
```ts
@Component({ template: `<app-form #f></app-form>` })
export class HostComponent {
  form = viewChild<FormComponent>('f');
  submit() { this.form()?.submit(); }
}
```

### d) Two unrelated components: **shared service** (signal/Subject)
```ts
@Injectable({ providedIn: 'root' })
export class MessageBus {
  private msg = signal('');
  message = this.msg.asReadonly();
  send(m: string) { this.msg.set(m); }
}
```

### e) Deep ancestor → far descendant: **DI** (`@Optional() @SkipSelf()`)

### f) Across whole app: **NgRx / Signal Store / State service**

> Senior interview answer: "Default to `@Input/@Output` (or `input`/`output`) for direct parent/child. Move to a shared injectable service for siblings. Reach for a state-management lib only when the data is global, derived, and cached."

---

## 3. Content Projection — `<ng-content>`

Lets a parent inject markup into your component.

### Simple
```ts
@Component({ selector: 'app-card',
  template: `<div class="card"><ng-content></ng-content></div>` })
export class CardComponent {}
```
```html
<app-card>Anything here lands inside the card.</app-card>
```

### Multi-slot
```ts
@Component({ selector: 'app-card', template: `
  <header><ng-content select="[card-header]"></ng-content></header>
  <main><ng-content></ng-content></main>
  <footer><ng-content select="[card-footer]"></ng-content></footer>
`})
```
```html
<app-card>
  <h2 card-header>Title</h2>
  <p>Body</p>
  <button card-footer>OK</button>
</app-card>
```

---

## 4. `viewChild` / `contentChild` (Signal Queries — Angular 17.2+)

```ts
input  = viewChild<ElementRef<HTMLInputElement>>('inp');
items  = viewChildren(ChildComponent);            // signal of array
header = contentChild<HeaderDirective>(HeaderDirective);

ngAfterViewInit() {
  this.input()?.nativeElement.focus();
}
```

Legacy decorator equivalent:
```ts
@ViewChild('inp') input!: ElementRef<HTMLInputElement>;
@ViewChildren(ChildComponent) items!: QueryList<ChildComponent>;
@ContentChild(HeaderDirective) header!: HeaderDirective;
```

`{ static: true }` if you need access in `ngOnInit` (not behind `*ngIf`).

---

## 5. View Encapsulation

Three modes:

| Mode | Behavior |
|---|---|
| `Emulated` (default) | Angular adds attribute selectors so component styles don't leak |
| `ShadowDom` | True Shadow DOM, real isolation, **opt-in** |
| `None` | Styles become global (don't use unless you mean it) |

```ts
@Component({ encapsulation: ViewEncapsulation.ShadowDom, ... })
```

Style ancestors via host:
```scss
:host { display: block; }
:host(.dark) { background: #111; }
:host-context(.print) .toolbar { display: none; }
```

---

## 6. Change Detection in Depth

Angular runs CD every time something **could** have changed (events, timers, HTTP, microtasks) via **Zone.js**. Each component's CD compares bound expressions and updates DOM.

Two strategies:

| Strategy | When CD runs | When to use |
|---|---|---|
| `Default` | Every time any event fires anywhere | OK for small apps |
| `OnPush` | Only when: (1) `@Input` reference changes, (2) event from this component fires, (3) `async` pipe emits, (4) signal in template changes, (5) `markForCheck()` called | Everywhere in serious apps |

```ts
@Component({ changeDetection: ChangeDetectionStrategy.OnPush })
```

With OnPush:
- Mutating an input object **won't** trigger CD — return a new object.
- Async data via `| async` or `toSignal` is auto-CD-safe.
- Manually trigger via `ChangeDetectorRef.markForCheck()` (preferred) or `.detectChanges()` (scoped sync).

> **Senior interview line**: "I default every component to `OnPush` and use signals or the `async` pipe — that eliminates 90% of perf surprises."

---

## 7. Signals (Angular 16+, stable & default in 18+)

```ts
count = signal(0);                                     // writable
double = computed(() => this.count() * 2);            // derived
effect(() => console.log('count is', this.count()));  // side-effect

this.count.set(5);
this.count.update(c => c + 1);
```

Signals integrate with templates: reading a signal in a template marks the component dirty automatically.

Bridges:
```ts
data$ = this.svc.list();
data  = toSignal(this.data$, { initialValue: [] });

value$ = toObservable(this.value);
```

---

## 8. `@Input` Setters / `ngOnChanges` / `computed`

Three ways to react to input changes (in increasing modernity):

```ts
// 1. Setter
@Input() set value(v: number) { this._v = v * 2; }

// 2. ngOnChanges
ngOnChanges(changes: SimpleChanges) {
  if (changes['value']) { /* react */ }
}

// 3. Signal input + computed (preferred)
value = input<number>(0);
doubled = computed(() => this.value() * 2);
```

---

## 9. Dynamic Components

Create components at runtime — modals, tabs, dashboards.

```ts
@Component({ template: `<ng-template #host></ng-template>` })
export class HostComponent {
  vc = viewChild('host', { read: ViewContainerRef });

  open() {
    const ref = this.vc()!.createComponent(MyModalComponent);
    ref.setInput('title', 'Hello');
    ref.instance.closed.subscribe(() => ref.destroy());
  }
}
```

Or **`*ngComponentOutlet`** for declarative:
```html
<ng-container *ngComponentOutlet="comp; inputs: { title: 'Hi' }"></ng-container>
```

---

## 10. Host Bindings & Listeners

```ts
@Component({
  selector: 'app-toggle',
  host: {
    '[class.active]': 'isActive()',
    '[attr.role]': '"switch"',
    '(click)': 'toggle()'
  }
})
```

Decorator form:
```ts
@HostBinding('class.active') get active() { return this.isActive; }
@HostListener('click') toggle() { this.isActive = !this.isActive; }
```

---

## 11. ng-template vs ng-container vs ng-content

| Tag | Renders? | Use |
|---|---|---|
| `<ng-template>` | **No** by default; rendered via `*ngTemplateOutlet` or directives | Reusable template fragments |
| `<ng-container>` | Groups elements without producing a DOM node | Multiple structural directives, conditional wrappers |
| `<ng-content>` | Slot for projected content | Reusable wrapper components |

```html
<ng-container *ngIf="user">
  <h1>{{ user.name }}</h1>
</ng-container>

<ng-template #empty>Nothing here.</ng-template>
<div *ngIf="items.length; else empty">…</div>
```

---

## 12. Smart vs Presentational Components

**Smart (container)**: knows about services, fetches data, has business logic.
**Presentational (dumb)**: pure inputs/outputs, no DI of services, easy to test/storybook.

Why: testability, reusability, performance (presentational components are trivially OnPush).

---

## 13. Standalone Component Imports

A standalone component lists what its template uses:

```ts
@Component({
  standalone: true,
  imports: [CommonModule, RouterLink, UserCardComponent, TitleCasePipe, MyDirective],
  template: `...`
})
```

Tree-shakable: unused imports drop out of the bundle.

---

## 14. Hydration & SSR (Angular Universal)

`ng new --ssr` or add via `ng add @angular/ssr`. Renders on Node, sends HTML to browser, then **hydrates** (no double-render).

```ts
bootstrapApplication(AppComponent, {
  providers: [provideClientHydration(), ...]
});
```

Senior topic: lazy hydration (Angular 18+) with `withIncrementalHydration()` — components hydrate on viewport / interaction.

---

## 15. Common Component Pitfalls

| Pitfall | Fix |
|---|---|
| Calling a method in template every CD | Move to signal / computed / pure pipe |
| Forgetting `track` in `@for` | Always provide a stable id |
| `subscribe` without cleanup | `async` pipe / `takeUntilDestroyed` |
| Reading `viewChild()` in `ngOnInit` | Use `ngAfterViewInit` (or `{ static: true }`) |
| Mutating `@Input` array | Replace with `[...arr, newItem]` |
| Heavy work in `ngOnChanges` | Use signals + `computed` |
| One component knows about routing, HTTP, DOM | Split smart/dumb |
| Animations in change-heavy template | Throttle / use `OnPush` |

---

## 16. Senior Interview Q&A

**Q1. Lifecycle order on first render?**
`constructor` → `ngOnChanges` → `ngOnInit` → `ngDoCheck` → `ngAfterContentInit` → `ngAfterContentChecked` → `ngAfterViewInit` → `ngAfterViewChecked`. Then on updates: `ngOnChanges` → `ngDoCheck` → … → `ngAfterViewChecked`. On removal: `ngOnDestroy`.

**Q2. When does OnPush NOT update?**
When you mutate a bound object without changing its reference, when an async non-`| async` operation updates state without `markForCheck`, or when an event fires outside Angular zone.

**Q3. Why not `*ngIf` plus a method?**
Method runs every CD cycle — caching via signal/computed is cheap; method calls aren't.

**Q4. What's `ng-container` for?**
A logical wrapper without producing DOM — combine multiple structural directives or wrap conditionally without an extra `<div>`.

**Q5. Difference between `@ViewChild` and `@ContentChild`?**
`ViewChild` queries the component's own template. `ContentChild` queries what was **projected into** the component via `<ng-content>`.

**Q6. Why prefer signals over `BehaviorSubject`?**
Signals are synchronous, glitch-free, integrate with CD (no manual `markForCheck`), and have first-class `computed`/`effect`. Subjects are still needed for async streams (HTTP, websockets).

**Q7. How would you build a reusable modal component?**
Standalone component, content projection for header/body/footer slots, output for close, opened via `*ngComponentOutlet` or `ViewContainerRef.createComponent`. Trap focus, handle ESC, restore focus on close, ARIA roles.

**Q8. How do smart and dumb components fit together?**
Smart fetches data via services and passes via inputs to dumb components, which emit events back. Smart maps events to commands.

---

## 17. Mental Model

> **A component is a black-box: inputs in, outputs out, content projected through `<ng-content>`. Lifecycle hooks let Angular tell you when to work. OnPush + signals + async pipe = fast, predictable. Smart for orchestration, dumb for UI.**
