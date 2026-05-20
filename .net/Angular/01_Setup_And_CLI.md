# 01 — Setup & Angular CLI

> **One-liner**: Angular CLI is the swiss army knife. Install it once, then almost everything you do in Angular is `ng <something>`.

---

## 1. Prerequisites

| Tool | Why | Install |
|---|---|---|
| **Node.js LTS (≥ 20.x)** | Runtime for CLI + dev server | `winget install OpenJS.NodeJS.LTS` |
| **npm / pnpm / yarn** | Package manager | Comes with Node |
| **Git** | Source control | `winget install Git.Git` |
| **VS Code** | Editor | `winget install Microsoft.VisualStudioCode` |
| **Angular Language Service** ext | Template intellisense | VS Code marketplace |

Verify:
```powershell
node -v          # v20.x or higher
npm -v
git --version
```

Angular 18+ requires Node ≥ 18.19; **Angular 19/20 require Node ≥ 20.11**.

---

## 2. Install the Angular CLI

```powershell
npm install -g @angular/cli
ng version
```

Check global location:
```powershell
npm root -g
Get-Command ng
```

Update CLI later:
```powershell
npm uninstall -g @angular/cli
npm install -g @angular/cli@latest
```

---

## 3. Create a New App

```powershell
ng new my-app
# prompts:
#   ? Which stylesheet?  CSS / SCSS / Sass / Less
#   ? Server-Side Rendering (SSR/SSG)?  No (or Yes for Angular Universal)
#   ? Strict mode?  Yes  (recommended)
#   ? Use standalone components?  Yes  (default since v17)
```

Non-interactive (CI):
```powershell
ng new my-app --routing --style=scss --ssr=false --standalone --skip-tests=false --strict
```

Folder structure (Angular 18+, standalone):
```
my-app/
├── src/
│   ├── app/
│   │   ├── app.component.ts          # root component (standalone)
│   │   ├── app.component.html
│   │   ├── app.component.scss
│   │   ├── app.config.ts             # provideRouter, provideHttpClient...
│   │   └── app.routes.ts             # Routes array
│   ├── main.ts                       # bootstrapApplication(AppComponent, appConfig)
│   ├── index.html
│   └── styles.scss
├── angular.json                      # workspace config
├── package.json
├── tsconfig.json / tsconfig.app.json
└── README.md
```

---

## 4. Run, Build, Test

```powershell
ng serve                              # dev server: http://localhost:4200, hot reload
ng serve --port 4300 --open --ssl     # custom port + open browser + https
ng build                              # production by default (Angular 17+)
ng build --configuration=development
ng test                               # karma + jasmine (or jest if configured)
ng e2e                                # e2e (Cypress / Playwright)
ng lint                               # if @angular-eslint installed
```

Output goes to `dist/my-app/`.

---

## 5. Generate Things — `ng generate` (alias `ng g`)

| Shortcut | Long | Generates |
|---|---|---|
| `ng g c users/user-list` | `--standalone` (default) | Component |
| `ng g s data` | service | Injectable service |
| `ng g d highlight` | directive | Attribute directive |
| `ng g p timeAgo` | pipe | Pipe |
| `ng g m admin --route admin --module app` | NgModule + lazy route (legacy) | Feature module |
| `ng g guard auth` | guard | Route guard (canActivate / canMatch / canDeactivate) |
| `ng g resolver user` | resolver | Route resolver |
| `ng g interceptor auth` | interceptor | HTTP interceptor |
| `ng g i user` | interface | Interface (model) |
| `ng g cl user` | class | Plain class |
| `ng g e role` | enum | Enum |
| `ng g lib shared` | library | Library project inside workspace |
| `ng g application admin-app` | app | Multi-app workspace |

Useful flags:
- `--dry-run` (`-d`) — preview without writing
- `--skip-tests` — no `.spec.ts`
- `--inline-template` / `--inline-style`
- `--change-detection=OnPush`
- `--export` (legacy modules)

---

## 6. Workspace Config — `angular.json`

Single workspace can hold multiple **projects** (apps + libraries). Key sections:

```jsonc
{
  "projects": {
    "my-app": {
      "architect": {
        "build": {
          "options": {
            "outputPath": "dist/my-app",
            "index": "src/index.html",
            "browser": "src/main.ts",
            "tsConfig": "tsconfig.app.json",
            "styles": ["src/styles.scss"],
            "scripts": []
          },
          "configurations": {
            "production": {
              "budgets": [
                { "type": "initial", "maximumWarning": "500kb", "maximumError": "1mb" }
              ],
              "outputHashing": "all",
              "optimization": true,
              "sourceMap": false
            }
          }
        }
      }
    }
  }
}
```

Bundle budgets fail the build when you bloat — keep them tight.

---

## 7. Environments

Pre-Angular 16 used `environments/environment.ts` files. Modern approach: **runtime config** (load JSON at startup) or **build-time replacement** via `fileReplacements`.

```jsonc
// angular.json -> configurations.production
"fileReplacements": [
  { "replace": "src/environments/environment.ts",
    "with":    "src/environments/environment.prod.ts" }
]
```

```ts
// src/environments/environment.ts
export const environment = {
  production: false,
  apiBase: 'https://localhost:5001/api'
};
```

---

## 8. Useful Global Commands

```powershell
ng update                              # show available upgrades
ng update @angular/core @angular/cli   # migrate to latest Angular
ng update @angular/core@19             # specific major
ng analytics off                       # opt out of telemetry
ng config schematics.@schematics/angular:component.changeDetection OnPush
ng cache clean                         # clear .angular cache (fixes weird issues)
ng doc Component                       # opens angular.io docs
```

---

## 9. Package Managers — npm vs pnpm vs yarn

| | npm | pnpm | yarn |
|---|---|---|---|
| Default with Node | yes | no | no |
| Speed | slow | **fastest** | fast |
| Disk usage | high | **content-addressable, lowest** | medium |
| Monorepo support | good | **best (workspaces)** | good |

Set CLI to use pnpm:
```powershell
ng config cli.packageManager pnpm
```

---

## 10. Standalone vs NgModule (since v14, default in v17+)

**Standalone components** import other components/directives/pipes directly — no `NgModule` needed.

```ts
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, UserListComponent],
  template: `<app-user-list/><router-outlet/>`
})
export class AppComponent {}
```

`main.ts` (no `AppModule`):
```ts
bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync()
  ]
});
```

> Senior interview line: "I default to standalone — fewer files, lazy loading by route directly, tree-shakable. NgModule is still supported but feels legacy."

---

## 11. Strict Mode (`tsconfig.json`)

Default `ng new --strict` enables:
- `strict: true` (all TS strict flags)
- `noImplicitOverride`
- `noFallthroughCasesInSwitch`
- `strictTemplates` in `angularCompilerOptions`

Never disable these in interview repos.

---

## 12. VS Code Setup

Extensions:
- **Angular Language Service** (official)
- **Angular Snippets** (John Papa)
- **ESLint**, **Prettier**, **EditorConfig**
- **Auto Rename Tag**, **Path Intellisense**

`settings.json`:
```jsonc
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": { "source.fixAll.eslint": "explicit" },
  "typescript.preferences.importModuleSpecifier": "relative"
}
```

---

## 13. First-Day Troubleshooting

| Problem | Fix |
|---|---|
| `ng: command not found` | Add `npm root -g` path; reopen shell |
| `EACCES`/permission errors on Windows | Run PowerShell as admin or use `nvm-windows` |
| Old Angular complains about Node version | Use **nvm-windows** to switch Node versions per project |
| `node-gyp` errors building native deps | Install windows-build-tools or use prebuilt binaries |
| Hot reload broken | `ng cache clean`, restart, check antivirus locking `.angular/cache` |
| HTTPS dev | `ng serve --ssl --ssl-cert=path --ssl-key=path` |
| CORS in dev | Add proxy: `ng serve --proxy-config proxy.conf.json` |

`proxy.conf.json`:
```jsonc
{
  "/api": {
    "target": "https://localhost:5001",
    "secure": false,
    "changeOrigin": true
  }
}
```

---

## 14. Senior-Level Setup Choices to Discuss in an Interview

- **pnpm + workspaces** for monorepo / shared libs.
- **Standalone components** by default; eliminate `AppModule`.
- **OnPush change detection** default via schematics config.
- **ESLint + Prettier + Husky + lint-staged** pre-commit hooks.
- **Bundle budgets** strict; CI fails on regression.
- **Renovate / Dependabot** for `ng update` PRs.
- **Nx** if monorepo grows past 3 apps.

---

## 15. Mental Model

> **CLI = Angular's compiler frontend, scaffolder, dev server, test runner, and deploy tool in one. Master `ng new / serve / generate / build / test / update` and you've already cleared the first 10 minutes of any Angular interview.**
