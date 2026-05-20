# 04 — Directives & Pipes

> **One-liner**: Directives change behavior or structure of existing elements; pipes transform values in templates.

---

## 1. Three Kinds of Directives

| Kind | Example | What it does |
|---|---|---|
| **Component** | `@Component` | A directive **with a template** — most common |
| **Attribute directive** | `[ngClass]`, `[ngStyle]`, custom `[appHighlight]` | Changes appearance/behavior of host |
| **Structural directive** | `*ngIf`, `*ngFor`, `*ngSwitch`, custom `*appShowIf` | Adds/removes elements from the DOM |

Components are technically directives — that's why they share decorators and lifecycle.

---

## 2. Built-in Attribute Directives

```html
<div [ngClass]="{ 'active': isActive, 'big': size > 10 }"></div>
<div [class.active]="isActive"></div>                         <!-- shorthand -->
<div [ngStyle]="{ color: theme === 'dark' ? '#fff' : '#000' }"></div>
<div [style.color]="theme === 'dark' ? '#fff' : '#000'"></div>
<input [(ngModel)]="name" />                                  <!-- needs FormsModule -->
```

---

## 3. Built-in Structural Directives (legacy syntax)

```html
<p *ngIf="loading; else done">…</p>
<ng-template #done>Done</ng-template>

<li *ngFor="let item of items; let i = index; trackBy: trackById">
  {{ i + 1 }}. {{ item.name }}
</li>

<div [ngSwitch]="role">
  <admin-panel *ngSwitchCase="'admin'"/>
  <user-panel  *ngSwitchCase="'user'"/>
  <guest-panel *ngSwitchDefault/>
</div>
```

Modern equivalent (Angular 17+) is **`@if`/`@for`/`@switch`** — see [02_Angular_Basics](02_Angular_Basics.md#5-built-in-control-flow-angular-17).

> The `*` is **syntactic sugar** for an `<ng-template>` wrapper. `*ngIf="x"` becomes `<ng-template [ngIf]="x">…</ng-template>`.

---

## 4. Custom Attribute Directive

```ts
import { Directive, ElementRef, HostBinding, HostListener, input } from '@angular/core';

@Directive({
  selector: '[appHighlight]',
  standalone: true
})
export class HighlightDirective {
  color = input<string>('yellow');

  constructor(private el: ElementRef<HTMLElement>) {}

  @HostListener('mouseenter') onEnter() { this.set(this.color()); }
  @HostListener('mouseleave') onLeave() { this.set(null); }

  private set(c: string | null) {
    this.el.nativeElement.style.backgroundColor = c ?? '';
  }
}
```
```html
<p appHighlight color="lightblue">Hover me</p>
```

Senior touches: use `Renderer2` instead of `nativeElement.style` for SSR safety:
```ts
constructor(private r: Renderer2, private el: ElementRef) {}
this.r.setStyle(this.el.nativeElement, 'background-color', c);
```

---

## 5. Custom Structural Directive

A structural directive manipulates an `<ng-template>` via `TemplateRef` + `ViewContainerRef`.

```ts
@Directive({ selector: '[appUnless]', standalone: true })
export class UnlessDirective {
  private hasView = false;
  constructor(private tpl: TemplateRef<unknown>,
              private vc: ViewContainerRef) {}

  @Input() set appUnless(condition: boolean) {
    if (!condition && !this.hasView) {
      this.vc.createEmbeddedView(this.tpl);
      this.hasView = true;
    } else if (condition && this.hasView) {
      this.vc.clear();
      this.hasView = false;
    }
  }
}
```
```html
<p *appUnless="isLoggedIn">Please sign in.</p>
```

Pass extra context via `microsyntax`:
```ts
@Input() set appShowIf(condition: boolean) { ... }
@Input() set appShowIfElse(tpl: TemplateRef<unknown>) { ... }
```

---

## 6. Directive Composition API (Angular 15+)

A component can pull in directives without HTML attributes:
```ts
@Component({
  hostDirectives: [
    { directive: HighlightDirective, inputs: ['color'], outputs: [] },
    CdkTrapFocus
  ],
  ...
})
export class FancyButtonComponent {}
```

Great for composing reusable behaviors (a11y, drag-drop) into many components.

---

## 7. Pipes — Transformations in Templates

```html
{{ price | currency:'USD':'symbol':'1.2-2' }}
{{ name  | uppercase }}
{{ list  | slice:0:5 }}
{{ data  | json }}
{{ when  | date:'yyyy-MM-dd HH:mm':'UTC' }}
{{ count | number:'1.0-2' }}
{{ percent | percent:'1.0-1' }}
{{ count | i18nPlural:{ '=0':'no items', '=1':'1 item', 'other':'# items' } }}
```

Chaining is left-to-right:
```html
{{ user.created | date | uppercase }}
```

---

## 8. The `async` Pipe

```html
<ng-container *ngIf="user$ | async as user; else loading">
  Hello {{ user.name }}
</ng-container>
<ng-template #loading>…</ng-template>
```

- Subscribes on init, **unsubscribes on destroy** — no leak risk.
- Triggers CD on emission.
- Works with `Promise` and `Observable`.

In OnPush components this is essentially mandatory for async data — or use `toSignal`.

---

## 9. Custom Pipe

```ts
@Pipe({ name: 'truncate', standalone: true, pure: true })
export class TruncatePipe implements PipeTransform {
  transform(value: string, max = 20, ellipsis = '…'): string {
    if (!value) return '';
    return value.length > max ? value.slice(0, max) + ellipsis : value;
  }
}
```
```html
{{ description | truncate:50 }}
```

---

## 10. Pure vs Impure Pipes

| | Pure (default) | Impure |
|---|---|---|
| Runs when | **Inputs reference change** | Every CD cycle |
| Perf | Fast | Risky in big lists |
| Use for | Most transforms | Filtering live arrays where you can't change the ref |

```ts
@Pipe({ name: 'filter', pure: false })
```

> Senior interview tip: prefer **pure** pipes; if you need filtering, do it in the component (with signals/computed) and template a pure list.

---

## 11. Why Pipes Beat Methods in Templates

```html
<!-- BAD: calls each CD -->
{{ formatPrice(price) }}
<!-- GOOD: cached by Angular until input changes -->
{{ price | currencyExt }}
```

Same with `*ngIf="bigComputation()"` — refactor into `computed()` signal.

---

## 12. `Renderer2` — DOM Without `nativeElement`

When SSR / Web Workers are in play, don't touch DOM directly. Use `Renderer2`:

```ts
constructor(private r: Renderer2, private el: ElementRef) {}

this.r.addClass(this.el.nativeElement, 'active');
this.r.setStyle(this.el.nativeElement, 'color', '#fff');
this.r.setAttribute(this.el.nativeElement, 'aria-pressed', 'true');
const unbind = this.r.listen(this.el.nativeElement, 'click', () => {});
```

---

## 13. Directive vs Component vs Pipe — When to Use Which

| Need | Use |
|---|---|
| New UI element with template | Component |
| Add behavior to existing element | Attribute directive |
| Add/remove DOM based on condition | Structural directive |
| Transform display value | Pipe |
| Compose reusable behaviors | `hostDirectives` |

---

## 14. A Few Powerful Real-World Directives to Build

- `*appPermission="'orders.write'"` — show only if user has permission
- `[appAutoFocus]` — focus on init
- `[appDebounceClick]` — prevent double-submit
- `[appClickOutside]` — emit when clicking outside (for menus/modals)
- `*appFeatureFlag="'newCheckout'"` — toggle features
- `[appTooltip]="text"` — light tooltip composition with CDK overlay
- `*appLet="user$ | async as user"` — local binding pattern

`*appLet`:
```ts
@Directive({ selector: '[appLet]', standalone: true })
export class LetDirective<T> {
  private ctx: { $implicit: T | null; appLet: T | null } = { $implicit: null, appLet: null };
  constructor(private tpl: TemplateRef<typeof this.ctx>, private vc: ViewContainerRef) {
    this.vc.createEmbeddedView(this.tpl, this.ctx);
  }
  @Input() set appLet(value: T) { this.ctx.$implicit = value; this.ctx.appLet = value; }
}
```

---

## 15. Pitfalls

| Pitfall | Fix |
|---|---|
| Impure pipe doing heavy work | Move to component logic / pure pipe |
| Directive modifying inputs in `ngOnInit` | Use `effect` / setter |
| `*ngIf` + `*ngFor` on same element | Wrap one in `<ng-container>` |
| Forgetting to add directive to component `imports` | TypeScript error in template |
| Directive reading DOM in constructor | Use `ngAfterViewInit` |
| Memory leak on `Renderer2.listen` | Capture returned `unlisten` and call it in `ngOnDestroy` |
| `ngClass` with object recreated each CD | Use `[class.active]` or stable ref |

---

## 16. Senior Interview Q&A

**Q1. What is the `*` in `*ngIf`?**
Microsyntax sugar that expands to an `<ng-template>` with the structural directive applied — the host element isn't created when the condition is false.

**Q2. Pure vs impure pipe?**
Pure runs only when input reference changes (default, fast). Impure runs every CD — dangerous on large lists.

**Q3. Why prefer pipes over template methods?**
Pipes cache by input reference; methods re-run on every CD cycle, hurting performance.

**Q4. Can you have multiple structural directives on one element?**
No — wrap with `<ng-container>` and put one on each.

**Q5. When to use `Renderer2`?**
Always in code that may run in SSR / Web Worker. It abstracts DOM operations.

**Q6. Difference between `[ngClass]` and `[class.foo]`?**
`[class.foo]="cond"` toggles a single class — fast. `[ngClass]` accepts strings/arrays/objects — more flexible.

**Q7. How does the `async` pipe avoid memory leaks?**
It subscribes on init and unsubscribes on destroy (`ngOnDestroy`). It also calls `markForCheck` on each emission so OnPush components update.

**Q8. Why use `hostDirectives` vs inheritance?**
Composition over inheritance — pull behaviors (e.g., focus trap, analytics) into many components without forcing a class hierarchy.

---

## 17. Mental Model

> **Directive = behavior on an existing element. Pipe = value transform in a template. Component = directive with a view. Pick the smallest tool that does the job. Pipes pure by default; directives via `host`/`Renderer2`; structural ones use `TemplateRef`+`ViewContainerRef`.**
