# Project Standards

These are the engineering standards for Social Tennis. They say **how well** the app must be built and how it must be verified. **What** it does is in `spec.md` (including the platform: an offline PWA built with .NET Blazor WebAssembly), and each increment of work is in a `taskN.md` file.

C# style, preferred test libraries and the open-source licence rule are in [codingStandard.md](codingStandard.md). Apart from those, project structure, libraries and helper scripts are the implementer's choice, as long as every rule here is met.

`spec.md` describes the finished product. The tasks build it up in order, so an early task can describe something a later task changes (for example a button that is later removed). Work is done in task order: read `spec.md` and the current `taskN.md` before starting, and implement the current task as written. Once every task is done, the app matches the spec. The docs are inputs: don't edit `spec.md` or the task files.

The words **must** and **never** mark rules. **Should** marks a strong default that needs a stated reason to break.

---

## 1. Solution

- The repository contains one solution that includes every project, so a single build command builds everything and a single test command runs every test.
- Production code and test code live in separate projects. Test code is never shipped in the app.
- Code is organised so that a reader can find a feature's logic, its UI and its tests without searching.
- The app must be runnable locally with one command, on **http://localhost:3000**, from a `launchSettings.json` profile.

## 2. Architecture

- **Logic is separate from UI.** Rules, validation, parsing and the schedule engine are plain .NET code that can be tested without a browser. Pages and components stay thin: they call that logic and render its results.
- **Algorithms are reproducible.** Anything random can be given a seed, so a test, or a bug report, can reproduce the exact same result.
- **Data stays on the device** (see `spec.md`). Storage access is isolated behind an interface so tests can replace it.
  - Saved data must stay readable by later versions of the app. Loading old data must never fail or lose players.
  - Multi-step flows save after every step, so closing the app resumes where the user left off.
- **Trimming:** the Release publish is trimmed. Everything, serialisation included, must work in the published build, not only in a Debug run.
- **Browser features** that only some browsers have may be used only after checking that they exist, and must have a fallback that works on iPhone Safari.
- **Testability:** UI behaviour that depends on time (such as press-and-hold) must be testable without waiting on, or racing, a real timer.

## 3. Offline and installation

- After the first load the app must work fully offline, including after being closed and re-opened with no connection.
- Offline support must keep working in the deployed build, including when it is served from a repository subpath. Any step that alters files after the build must not break the offline cache.
- The app must install from Safari (Share → Add to Home Screen) with its own name and an icon that displays correctly on iOS, and open full screen.
- New versions must reach installed copies when they next open with a connection, without the user losing data.

## 4. UI and layout

- The app is **mobile first and portrait**. The smallest supported screen is **iPhone SE (375 × 667)**. Pages must not scroll horizontally.
- The UI is clean and uncluttered. It doesn't need to imitate a native app.
- Fixed elements (header, toolbar) and dialogs must stay clear of the notch and the home indicator.
- **Dialogs** are centred on the screen and never anchored to the bottom, where the toolbar and home bar are. The whole dialog (title, message and every button) must be visible on an iPhone SE wherever the page is scrolled to. If the content is too tall, the dialog scrolls inside itself and its buttons stay reachable.
- **Touch:** gestures such as drag and press-and-hold must work with a finger on iOS Safari, and must not trigger text selection or the iOS callout menu.
- **Theme:** light and dark themes both work. The page must not flash the wrong theme as it opens.
- **Contrast:** text has at least 4.5:1 contrast against its background in both themes. Anything read courtside at night must be large and high contrast.
- **Accessibility:** every control has an accessible name (a visible label, `aria-label`, or button and link text), and semantic elements are used.

## 5. Code quality enforcement

How to write the code is in [codingStandard.md](codingStandard.md). This section is about enforcing it in the build.

- An `.editorconfig` and **StyleCop** analyzers apply to **every** project in the repo. The rules from the coding standard that a tool can check are written down in those config files, so the build enforces them.
- **Warnings are errors** in Release builds, including publish.
- Analyzer warnings are fixed, never suppressed to get a build through. A repo-wide rule is changed only on purpose, with the reason recorded next to it.

## 6. Testing standards

**Every feature and every bug fix comes with automated tests.** A requirement in the spec or a task isn't done until a test shows it working. Tests are written alongside the code, not afterwards.

### 6.1 Unit tests

- All logic has unit tests: rules, validation, parsing, sorting and searching, storage round-trips, edge cases and error paths.
- Each test checks one behaviour, has a name that says what it checks, and doesn't depend on other tests or on run order.
- Tests never depend on the wall clock or on unseeded randomness.
- **The schedule rules are tested for all combinations:** every supported number of players and courts, across several seeds. The tests check every rule in the spec, in particular that nobody partners the same player twice in a session and nobody is on more than one court in a round. Results are checked by test code written independently of the engine, not by calling the engine's own helpers.

### 6.2 Tests in an actual browser

The app is also tested **in an actual browser**, end to end:

- Run against the **published Release build**, the same output that is deployed, with offline support active. Not the development server.
- Use **Safari's engine (WebKit)** by default, since that is what runs on iPhone, at **iPhone** screen sizes, with iPhone SE included for layout checks.
- Each test starts from **empty storage**, like a fresh install.
- Drive the UI the way a user does, by visible text, labels and roles, with real gestures (a real press-and-hold, a real drag).
- Cover, for every task: the main user journeys, **data persisting when the app is closed and re-opened**, **working offline** (with the server actually unreachable), dialogs fitting on the smallest screen, and the version shown after a stamped publish.
- Check that schedule generation stays fast enough to use courtside, on the largest realistic session, in the browser's WebAssembly runtime.
- The browser tests must be runnable with one command, with no manual setup beyond a first-time browser download.

## 7. Build and quality gates

There must be **zero warnings**:
- in Debug and Release builds,
- for every project on disk (after changing repo-wide config, rebuild all of them, not just the one being worked on),
- and in the IDE as well as the command line. Some diagnostics, such as naming rules, only appear in the IDE (VS Code's Problems tab), so an automated check must catch them too.

### Definition of done

A task is complete only when **all** of these hold:

1. Every requirement in the task has tests (section 6), and they pass.
2. Builds have **zero warnings and zero errors** as described above.
3. The full test suite passes, browser tests included.
4. The agent notes (`CLAUDE.md` or equivalent) are updated where the code changed, including any decision made where the spec or task left a choice open.

## 8. Versioning and deployment

- The app is hosted as static files on **GitHub Pages**, under the repository subpath.
- Every push to `master` builds and deploys automatically. The pipeline runs the tests and does not deploy if they fail. Deep links must work on the hosted site.
- Every deploy **stamps the version automatically**, along with the commit and release date, and the About page shows them. Local builds show a fixed development version.

## 9. Source control

- `master` is the release branch, and every push to it deploys. There is no `main` branch.
- `develop` is the integration branch.
- Each task is done on a `feature/<name>` branch (for example `feature/task5`) cut from `develop` and merged back into it. `develop` is released to `master` through a pull request.
- Commit messages have a short imperative subject naming the task or change.
- Build and test output is never committed.
