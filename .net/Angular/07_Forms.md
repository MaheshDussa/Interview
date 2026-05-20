# 07 — Forms (Template-driven & Reactive)

> **One-liner**: Use **Reactive Forms** for anything non-trivial. Template-driven is fine for quick login forms. Master `FormGroup` + validators + cross-field validation + typed forms and you've handled 95% of real-world Angular forms.

---

## 1. Two Form Flavors

| | Template-driven | Reactive |
|---|---|---|
| Where logic lives | Template | TS class |
| Setup | `FormsModule` + `[(ngModel)]` | `ReactiveFormsModule` + `FormGroup` |
| Validation | Directives in template | Validator functions/arrays in code |
| Async validators | Awkward | First-class |
| Dynamic forms | Painful | Easy (`FormArray`) |
| Strong typing | Weak | **Strongly typed** (Angular 14+) |
| Testability | Harder | Easy |
| Best for | Simple inputs (login) | Anything serious |

> Senior interview answer: "Default to **Reactive Forms** — strongly typed, testable, scales to dynamic forms and async validation."

---

## 2. Template-Driven Quick Start

```ts
@Component({ standalone: true, imports: [FormsModule], template: `
  <form #f="ngForm" (ngSubmit)="save(f.value)">
    <input name="email" [(ngModel)]="model.email" required email />
    <input name="password" [(ngModel)]="model.password" required minlength="8" type="password" />
    <button [disabled]="f.invalid">Login</button>
  </form>` })
export class LoginComponent {
  model = { email: '', password: '' };
  save(v: any) { /* ... */ }
}
```

`#f="ngForm"` exposes the `NgForm` directive (state: `valid`, `dirty`, `touched`, `value`).

---

## 3. Reactive Forms — The Real Deal

```ts
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()">
      <input formControlName="name" placeholder="Name" />
      @if (form.controls.name.touched && form.controls.name.invalid) {
        <small>Required, 2+ chars</small>
      }

      <input formControlName="email" placeholder="Email" />
      <button [disabled]="form.invalid">Save</button>
    </form>` })
export class ProfileComponent {
  private fb = inject(FormBuilder).nonNullable;

  form = this.fb.group({
    name:  this.fb.control('', [Validators.required, Validators.minLength(2)]),
    email: this.fb.control('', [Validators.required, Validators.email])
  });

  submit() {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();  // strongly typed!
    console.log(value);                     // { name: string; email: string }
  }
}
```

`FormBuilder.nonNullable` ⇒ values are non-null even after `reset()`. Strongly typed inference (Angular 14+).

---

## 4. Building Blocks

| Class | Use |
|---|---|
| `FormControl<T>` | A single field |
| `FormGroup<T>` | Object of controls |
| `FormArray<T>` | Dynamic list of controls |
| `FormBuilder` | Sugar to create above |
| `FormBuilder.nonNullable` | Same, but value is non-null |

```ts
form = this.fb.group({
  email: this.fb.control('', { validators: [Validators.required, Validators.email] }),
  addresses: this.fb.array<FormGroup>([])
});
```

---

## 5. Built-in Validators

```ts
Validators.required
Validators.requiredTrue
Validators.email
Validators.min(0)   Validators.max(100)
Validators.minLength(3)   Validators.maxLength(50)
Validators.pattern(/^\d{5}$/)
Validators.nullValidator
```

Combine:
```ts
this.fb.control('', [Validators.required, Validators.email])
```

---

## 6. Custom Sync Validator

```ts
export const noBadWords: ValidatorFn = (c: AbstractControl) =>
  /badword/i.test(c.value) ? { badWord: true } : null;

this.fb.control('', [Validators.required, noBadWords]);
```

Validator with config (factory):
```ts
export function forbidden(words: string[]): ValidatorFn {
  return (c: AbstractControl) =>
    words.includes(String(c.value).toLowerCase()) ? { forbidden: true } : null;
}
```

---

## 7. Custom Async Validator

```ts
@Injectable({ providedIn: 'root' })
export class UsernameTaken {
  private http = inject(HttpClient);
  validate: AsyncValidatorFn = (c) =>
    this.http.get<boolean>(`/api/users/exists?u=${c.value}`).pipe(
      map(taken => taken ? { taken: true } : null),
      catchError(() => of(null))
    );
}

// usage
username = this.fb.control('', {
  validators: [Validators.required],
  asyncValidators: [inject(UsernameTaken).validate],
  updateOn: 'blur'        // don't hammer the API
});
```

While async runs, control status is `'PENDING'`.

---

## 8. Cross-Field Validation (Group-level)

```ts
const matchPasswords: ValidatorFn = (g: AbstractControl) => {
  const p1 = g.get('password')?.value;
  const p2 = g.get('confirm')?.value;
  return p1 === p2 ? null : { mismatch: true };
};

form = this.fb.group({
  password: this.fb.control('', Validators.required),
  confirm:  this.fb.control('', Validators.required)
}, { validators: matchPasswords });
```

Show error:
```html
@if (form.errors?.['mismatch'] && form.controls.confirm.touched) {
  <small>Passwords don't match</small>
}
```

---

## 9. `FormArray` — Dynamic Lists

```ts
form = this.fb.group({
  phones: this.fb.array<FormControl<string>>([
    this.fb.control('', Validators.required)
  ])
});

get phones() { return this.form.controls.phones; }
addPhone()   { this.phones.push(this.fb.control('', Validators.required)); }
removePhone(i: number) { this.phones.removeAt(i); }
```

Template:
```html
<div formArrayName="phones">
  @for (ctrl of phones.controls; track $index) {
    <input [formControlName]="$index" />
    <button type="button" (click)="removePhone($index)">x</button>
  }
</div>
<button type="button" (click)="addPhone()">Add</button>
```

---

## 10. Control State & API

| Property | Meaning |
|---|---|
| `value` / `getRawValue()` | Current value (raw includes disabled) |
| `valid` / `invalid` | Validation state |
| `dirty` / `pristine` | User has typed? |
| `touched` / `untouched` | Field blurred? |
| `disabled` / `enabled` | `control.disable()` / `enable()` |
| `status` | `'VALID' \| 'INVALID' \| 'PENDING' \| 'DISABLED'` |
| `errors` | `{ required: true, email: true }` or `null` |
| `valueChanges` | Observable of value |
| `statusChanges` | Observable of status |

```ts
form.valueChanges.pipe(debounceTime(300), takeUntilDestroyed())
  .subscribe(v => this.autosave(v));
```

Setting values:
```ts
form.setValue({ name: 'a', email: 'b' });   // requires ALL fields
form.patchValue({ name: 'a' });             // partial OK
form.reset();
```

---

## 11. `updateOn` Strategy

```ts
this.fb.control('', { validators, updateOn: 'change' | 'blur' | 'submit' });
this.fb.group({ ... }, { updateOn: 'blur' });
```

Use `blur` for fields that hit network (debounces validation), `submit` for "validate everything on submit" forms.

---

## 12. Reusable ControlValueAccessor (Custom Input)

```ts
@Component({
  selector: 'app-rating',
  standalone: true,
  template: `
    @for (s of [1,2,3,4,5]; track s) {
      <span (click)="set(s)" [class.on]="s <= value">★</span>
    }`,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => RatingComponent), multi: true }
  ]
})
export class RatingComponent implements ControlValueAccessor {
  value = 0;
  onChange = (v: number) => {};
  onTouched = () => {};

  writeValue(v: number): void { this.value = v ?? 0; }
  registerOnChange(fn: any): void { this.onChange = fn; }
  registerOnTouched(fn: any): void { this.onTouched = fn; }
  setDisabledState?(d: boolean) { /* ... */ }

  set(v: number) { this.value = v; this.onChange(v); this.onTouched(); }
}
```

Usage:
```html
<app-rating formControlName="stars"></app-rating>
```

> Senior point: any input that wraps a native input or owns its UI should implement `ControlValueAccessor` so it plays nicely with `formControlName`, validation, and disabled state.

---

## 13. Error Display Patterns

Centralize:
```html
<input formControlName="email" />
<app-field-error [control]="form.controls.email"></app-field-error>
```

```ts
@Component({
  selector: 'app-field-error', standalone: true,
  template: `
    @if (control.touched && control.errors; as e) {
      @if (e['required']) { <small>Required</small> }
      @if (e['email'])    { <small>Invalid email</small> }
      @if (e['minlength']){ <small>Min {{ e['minlength'].requiredLength }} chars</small> }
    }`
})
export class FieldErrorComponent {
  control = input.required<AbstractControl>();
}
```

---

## 14. Performance Tips

- **`OnPush`** + reactive forms work great together.
- Use `updateOn: 'blur'` for expensive validators.
- Don't bind `form.valid` directly into many places — use a computed signal.
- Disable instead of removing controls if you need to keep values.
- Don't subscribe to `valueChanges` in template via async pipe for every keystroke unless needed.

---

## 15. Common Pitfalls

| Pitfall | Fix |
|---|---|
| `setValue` throws "Must supply value for all controls" | Use `patchValue` or include all keys |
| Disabled control missing from `value` | Use `getRawValue()` |
| Async validator races | `updateOn: 'blur'` + cancel previous via `switchMap` |
| `valueChanges` leaks subscription | `takeUntilDestroyed()` / `async` pipe |
| Form is `invalid` but no visible errors | Field is `untouched` — call `markAllAsTouched()` on submit |
| Custom input doesn't work with `formControlName` | Implement `ControlValueAccessor` |
| `nonNullable` not used + `.reset()` makes value null | `fb.nonNullable.control(...)` |
| Cross-field validator runs on parent group but error appears on child | Check `form.errors`, not `control.errors` |

```ts
submit() {
  this.form.markAllAsTouched();
  if (this.form.invalid) return;
  // ...
}
```

---

## 16. Senior Interview Q&A

**Q1. When would you choose template-driven over reactive?**
Very simple forms (login, search box). Or when the team only knows template-driven. Reactive scales better for everything else.

**Q2. How are typed forms in Angular 14+ different?**
`FormControl<string>`, `FormGroup<{ name: FormControl<string>; ... }>`. `value` and `getRawValue()` are inferred, eliminating `any`.

**Q3. Difference between `value` and `getRawValue()`?**
`value` excludes disabled controls; `getRawValue()` includes them. Use the latter on submit.

**Q4. How do you do cross-field validation?**
Pass a `ValidatorFn` to the parent group's options; the function inspects `group.get('a').value` vs `group.get('b').value` and returns an error object.

**Q5. Async validator gotchas?**
They run after sync validators pass. Set `updateOn: 'blur'`. Cancel previous requests with `switchMap` to avoid races.

**Q6. How does `ControlValueAccessor` work?**
Four methods (`writeValue`, `registerOnChange`, `registerOnTouched`, optional `setDisabledState`) let your custom input plug into Forms — `formControl` calls them to read/write values and notify changes.

**Q7. How to validate on submit only?**
Set `updateOn: 'submit'` on the group, or skip validation UI and call `markAllAsTouched()` in submit handler.

**Q8. How would you build a wizard with multiple steps?**
A `FormGroup` per step inside a parent `FormGroup`. Navigate by step index, validate per group. On final step, submit `getRawValue()`.

**Q9. How to test a reactive form?**
Instantiate the component via `TestBed`, set values via `patchValue`, assert `form.valid`, simulate clicks. No DOM needed for logic-only tests.

**Q10. How to debounce field validation calls to the server?**
`updateOn: 'blur'` on the control + use `switchMap` inside the async validator to cancel in-flight requests.

---

## 17. Cheat Sheet

```ts
this.fb.nonNullable.group({
  name:  this.fb.control('', [Validators.required, Validators.minLength(2)]),
  email: this.fb.control('', [Validators.required, Validators.email]),
  tags:  this.fb.array<FormControl<string>>([])
}, { validators: matchEmails, updateOn: 'blur' });

// Common ops
form.controls.name.setValue('Ada');
form.controls.name.markAsTouched();
form.markAllAsTouched();
form.reset();
form.getRawValue();
form.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe();
```

---

## 18. Mental Model

> **Reactive Forms = a tree of `AbstractControl`s with `value`, `status`, `errors`, observables. Wire to template with `formControlName`. Validators are pure functions (sync) or return Observables (async). Custom inputs implement `ControlValueAccessor`. With typed forms + `FormBuilder.nonNullable`, you get the same safety as the rest of strict-mode Angular.**
