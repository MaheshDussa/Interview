# 10 — Performance & Change Detection

> **One-liner**: 95% of Angular performance wins come from **OnPush + signals + `track` + pure pipes + lazy loading + bundle hygiene**. Profile before optimizing; don't over-engineer.

---

## 1. How Change Detection Works

Angular wraps the app in **Zone.js**, which monkey-patches async APIs (timers, events, HTTP). When any of them fire, Zone tells Angular "something might have changed" → Angular runs CD top-down on the component tree, comparing bound expressions against last values and updating the DOM.

Each component has a `LView`. CD walks parent → children, dirty-checks bindings, runs lifecycle hooks (`ngAfterViewChecked`, etc.).

Modern Angular is moving toward **zoneless** (signals drive CD directly) — preview in v18, stable target in v19/20.

---

## 2. CD Strategies

| Strategy | When CD runs for this component |
|---|---|
| **`Default`** | Whenever ANY event fires anywhere in the app |
| **`OnPush`** | Only when: (1) `@Input` reference changes, (2) DOM event from this component or its children, (3) `async` pipe / `toSignal` emits, (4) signal read in template changes, (5) `ChangeDetectorRef.markForCheck()` called |

```ts
@Component({ changeDetection: ChangeDetectionStrategy.OnPush })
```

Set default via schematics:
```powershell
ng config schematics.@schematics/angular:component.changeDetection OnPush
```

> Senior interview: "Default `OnPush` everywhere. The only times CD becomes confusing are when you mutate inputs without changing reference, or use third-party libs that update state outside Angular."

---

## 3. The OnPush Mental Model

OnPush components are "lazy" — they assume nothing changes unless told. To **tell** them:
- Pass a **new reference** for bound objects/arrays
- Use the `async` pipe / `toSignal`
- Read a **signal** in the template
- Call `cdr.markForCheck()` after async work that mutated state

```ts
// BAD — mutation, no CD
this.items.push(item);

// GOOD — new array reference
this.items = [...this.items, item];

// BEST — signal
this._items.update(arr => [...arr, item]);
```

---

## 4. `markForCheck` vs `detectChanges`

| | `markForCheck()` | `detectChanges()` |
|---|---|---|
| What | Marks the component (and ancestors) as **dirty**; next CD pass will run | Synchronously runs CD on this view (and children) **now** |
| When | After async work outside Angular | When you need a sync DOM update before something else (rare) |
| Cost | Cheap | Expensive — bypasses scheduling |

`ChangeDetectorRef.detach()` + manual `detectChanges` is an advanced optimization for charts/grids that update very fast.

---

## 5. Zone.js Pitfalls & `runOutsideAngular`

Heavy event sources (scroll, mousemove, requestAnimationFrame) trigger CD on every fire by default — kills perf.

```ts
private ngZone = inject(NgZone);

ngOnInit() {
  this.ngZone.runOutsideAngular(() => {
    window.addEventListener('scroll', this.onScroll);   // no CD
  });
}

// Re-enter zone only when you need CD
onScroll = () => {
  if (this.shouldUpdate()) {
    this.ngZone.run(() => this.updateUi());
  }
};
```

Same pattern for third-party libs (charts, maps).

---

## 6. Track Functions for `@for` / `*ngFor`

```html
@for (u of users(); track u.id) { … }
<!-- legacy -->
<li *ngFor="let u of users; trackBy: trackById">…</li>
```
```ts
trackById = (_: number, u: User) => u.id;
```

Without `track`, Angular destroys and recreates DOM on every list change — terrible for big tables. Always use a **stable identity**.

---

## 7. Pure Pipes Over Methods in Templates

Methods in templates run on **every CD cycle**:
```html
{{ formatPrice(item) }}        <!-- runs every CD -->
{{ item | priceFormat }}        <!-- pure pipe caches by input -->
```

Or — even better — `computed(() => format(this.item()))`.

---

## 8. `async` Pipe vs Manual Subscription

```html
<!-- BAD with OnPush -->
{{ user.name }}     <!-- you fetched in ngOnInit, set this.user — but CD doesn't fire -->

<!-- GOOD -->
{{ user$ | async }}.name
{{ user().name }}    <!-- signal -->
```

The `async` pipe automatically calls `markForCheck` when the stream emits.

---

## 9. Lazy Loading & Code Splitting

Each lazy route/component becomes its own chunk:
```ts
{ path: 'reports', loadComponent: () => import('./reports/reports').then(m => m.Reports) }
```

Also lazy-load **heavy dependencies** inside event handlers:
```ts
async exportPdf() {
  const { jsPDF } = await import('jspdf');     // chunk loaded on click
  new jsPDF().text('Hello', 10, 10).save();
}
```

Use `withPreloading(PreloadAllModules)` or a custom strategy to fetch chunks during idle.

---

## 10. Bundle Hygiene

- Set strict **budgets** in `angular.json` (`maximumError`: 1 MB initial).
- Run `ng build --stats-json` then **Source Map Explorer** / **webpack-bundle-analyzer** to see what's heavy.
- Avoid full-library imports (`import * as _ from 'lodash'`) — use `lodash-es` per-method imports.
- Drop **moment.js** for **date-fns** or native `Intl.DateTimeFormat`.
- RxJS: import only operators you use (handled automatically with treeshaking ≥ rxjs 7).
- Use **standalone components** + **`loadComponent`** so unused features stay out of bundles.

```powershell
npx source-map-explorer dist/my-app/browser/*.js
```

---

## 11. Image Optimization — `NgOptimizedImage`

```html
<img ngSrc="banner.jpg" width="1200" height="400" priority />
```

Gives you: lazy loading, `srcset`/`sizes`, fetch priority, layout shift warnings, optional `LoaderConfig` for CDN URLs.

Add a loader:
```ts
provideImgixLoader('https://cdn.contoso.com/')
```

---

## 12. Virtual Scrolling for Big Lists

```ts
import { ScrollingModule } from '@angular/cdk/scrolling';
```
```html
<cdk-virtual-scroll-viewport itemSize="40" class="vp">
  @for (u of users; track u.id) {
    <div>{{ u.name }}</div>
  }
</cdk-virtual-scroll-viewport>
```

Renders only the visible rows + a buffer. Essential for lists >500 items.

---

## 13. Memoization, Caching, & Deduping HTTP

- `shareReplay({ bufferSize: 1, refCount: false })` for app-wide caches.
- Service Worker (`ng add @angular/pwa`) for offline + asset caching.
- `HttpInterceptor` cache for idempotent GETs.

---

## 14. Server-Side Rendering & Hydration

```powershell
ng add @angular/ssr
```

Gives you:
- Server-rendered HTML for fast first paint + SEO
- **Hydration** (no double render) via `provideClientHydration()`
- Incremental hydration (Angular 18+): components hydrate when visible / interacted

```ts
providers: [
  provideClientHydration(withEventReplay()),     // event replay during hydration
  // 18+: withIncrementalHydration()
]
```

---

## 15. Zoneless Angular (Preview)

```ts
providers: [provideExperimentalZonelessChangeDetection()]
```

Removes Zone.js entirely. CD is driven by **signal reads** and explicit notifications. Smaller bundle (~30 kB), less overhead.

Caveat: third-party libs that relied on Zone-patched async may need updates.

---

## 16. Web Workers for CPU Work

```powershell
ng g web-worker app.worker
```

Generates a `.worker.ts` file and wires it to the build. Move CPU-heavy work off the main thread (parsing, image processing, crypto).

---

## 17. Profiling Tools

- **Angular DevTools** (Chrome ext) — Profiler shows CD timings per component, bindings.
- **Chrome Performance** panel — flamegraph, long tasks, layout shifts.
- **Lighthouse / PageSpeed** — LCP, INP, CLS scores.
- **Bundle stats**: `ng build --stats-json` + source-map-explorer.

Find:
- Components running CD too often (large CD count)
- Long tasks > 50ms
- Big JS chunks
- Unused imports

---

## 18. Common Performance Pitfalls

| Pitfall | Fix |
|---|---|
| Whole app on default CD | Switch to OnPush + signals |
| `[ngClass]="getClass()"` with method | Compute via signal/computed or use `[class.x]` |
| Big list re-rendering every keystroke | `track` + OnPush + virtual scroll |
| `@for` over filtered array recomputed each CD | Memoize in `computed` |
| `subscribe` in template-bound function | Use `async` pipe / `toSignal` |
| Importing all of lodash/material/firestore | Per-module imports, tree-shakable libs |
| Loading entire app on first navigation | Lazy routes + preloading strategy |
| Chart libraries inside Angular zone | `runOutsideAngular` |
| Re-rendering when `@Input` object reference changes but nothing else does | Use signal `input()` + `computed` |
| Mutating arrays/objects | Replace with new ref |
| `setTimeout` inside CD-heavy component | Wrap in `runOutsideAngular` if no UI update needed |

---

## 19. Senior Interview Q&A

**Q1. Why does OnPush improve performance?**
Skips CD for components whose inputs haven't changed and whose internal state hasn't signaled a change. Cuts the work in proportion to how many parts of the tree are static.

**Q2. What triggers CD in Default vs OnPush?**
Default: any zone-patched async event anywhere. OnPush: input ref change, event from that subtree, async pipe emission, signal read change, explicit `markForCheck`.

**Q3. Why is `track` mandatory in `@for`?**
Without a stable key, Angular destroys/recreates DOM each iteration on changes — terrible perf and breaks animations/focus.

**Q4. `markForCheck` vs `detectChanges`?**
`markForCheck` flags the component dirty for the **next** CD cycle. `detectChanges` runs CD **now** synchronously. Almost always you want `markForCheck`.

**Q5. How does the `async` pipe fix OnPush issues?**
It subscribes, stores the latest value, and calls `markForCheck()` on each emission — so OnPush updates correctly.

**Q6. What is `runOutsideAngular`?**
A way to register listeners/timers that don't trigger CD on every fire. Re-enter via `ngZone.run()` when state needs to update UI.

**Q7. Signals vs OnPush — do you still need OnPush?**
With signals + standalone, OnPush is essentially the default behavior. In the upcoming zoneless mode, the distinction disappears — CD runs only when signals change.

**Q8. How to debug a slow page?**
Angular DevTools Profiler: spot components with high CD counts. Chrome Performance: find long tasks. Bundle analyzer: find heavy chunks. Lighthouse: check LCP/INP.

**Q9. How to handle 100k-row tables?**
Server pagination + virtual scrolling + OnPush + `track`. Avoid binding expensive expressions. Render with CDK Virtual Scroll or `ag-Grid` for advanced needs.

**Q10. How would you cut JS bundle size?**
Audit with source-map-explorer; replace heavy libs (moment → date-fns); per-method imports; lazy-load features; ensure standalone tree-shaking; set tight budgets.

---

## 20. Cheat Sheet

```ts
changeDetection: ChangeDetectionStrategy.OnPush
trackById = (_:number, u:User) => u.id
ngZone.runOutsideAngular(() => …)
cdr.markForCheck()
provideClientHydration()
provideExperimentalZonelessChangeDetection()
```

```html
@for (u of users(); track u.id) {}
<img ngSrc="..." width="100" height="100" priority />
<cdk-virtual-scroll-viewport itemSize="40">…</cdk-virtual-scroll-viewport>
{{ stream$ | async }}
```

---

## 21. Mental Model

> **Performance = fewer CD passes × cheaper passes × smaller bundle × less main-thread work. Defaults: OnPush + signals + `track` + lazy routes + image opt + bundle budgets. Reach for `runOutsideAngular`, virtual scroll, web workers, SSR/hydration, and zoneless when profiling demands it. Always measure first.**
