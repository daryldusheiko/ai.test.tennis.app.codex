# Repository Guidelines

## Authoritative Development Inputs

Before implementation, read these documents in full:

- [Coding standard](app.codex.tennis/docs/codingStandard.md): C# conventions, licensing and preferred test libraries.
- [Project standards](app.codex.tennis/docs/projectStandards.md): architecture, quality gates, verification and deployment.
- [Product specification](app.codex.tennis/docs/spec.md): required behaviour, exact UI wording and schedule rules.
- The current `app.codex.tennis/docs/taskN.md`: the increment being implemented.

These documents govern development; this file is a reminder, not a replacement. Implement tasks in order and implement the current task as written. Early tasks can deliberately describe behaviour changed by later tasks; the finished product must match the specification. Do not edit `spec.md` or task files. Do not silently relax requirements to make implementation or verification easier. Record reasons for departures from strong defaults and resolve genuine conflicts explicitly.

## Repository Structure and Current Baseline

The app currently lives in `app.codex.tennis/`, with project file `app.codex.tennis/app.codex.tennis.csproj`. It is a .NET 10 Blazor WebAssembly PWA with the navigation shell, player management and Task 3 schedule creation/review/play. `SocialTennis.slnx` contains the app and the separate NUnit/WebKit project in `tests/SocialTennis.Tests/`. Player-level schedule editing and round controls are later increments.

- `Program.cs` configures startup and dependency injection; `App.razor` handles routing.
- `Players/` contains immutable player records, pure validation/search/sort rules, versioned JSON and the browser storage boundary.
- `Scheduling/` contains session records, court validation, the seeded engine, ordering and browser storage.
- `Components/` contains the shared schedule cards.
- `Pages/` contains routable components; `Layout/` contains shared layout, navigation and scoped styles.
- `wwwroot/` contains publicly accessible client assets.
- `Properties/launchSettings.json` defines development profiles.
- `docs/` contains the standards, specification and ordered task files.

During implementation, maintain one solution containing every project, with production and test code in separate projects. Test code must never ship in the app. Organise features so their logic, UI and tests are easy to find. Keep these notes current as structure and commands change.

## Product and Architecture

- Build Social Tennis as an offline, portrait iPhone PWA using C# and Blazor WebAssembly. There is no backend; players, notes, ranks and schedules stay on the device in browser storage. Never put credentials in client code, public assets or committed configuration.
- Keep rules, validation, parsing and scheduling in plain .NET code testable without a browser. Keep components thin: they call logic and render results.
- Isolate storage behind an interface. Keep old saved data readable without losing players. Save multi-step flows after every step so reopening resumes them, and preserve data during updates.
- Make random algorithms reproducible with seeds. Make time-dependent UI behaviour testable without waiting on or racing real timers.
- Release publishing is trimmed; all functionality, including serialisation, must work in published output.
- Detect optional browser capabilities before use and provide iPhone Safari fallbacks, including backup and restore.
- Support full offline use after first load, including closing and reopening without a connection. Preserve offline cache integrity when stamping or otherwise changing build assets and when hosting under a repository subpath.
- Support Safari Add to Home Screen with the specified names, tennis-ball icon and full-screen display. Installed copies receive updates when next opened online without data loss.

Read the full specification for each affected feature, including exact labels, messages and state transitions. Preserve these core invariants:

- Navigation is Play, Players, Schedule, About; startup routing depends on whether a schedule is in play.
- Only one schedule exists. Generate up to six rounds without repeat partners, reducing the round count when necessary. Require at least four players, fill doubles first, handle leftovers, distribute sitting out/three-player games/singles fairly, respect opponent and singles-rank limits, and order courts numerically as specified.
- Editing respects pinned positions and completed state, swaps stay within a round, and re-scheduling honours fairness, accepted conflicts and user edits as specified. Completed rounds cannot change. Play provides the specified read-only schedule and round controls.
- Player validation, searching, sorting, import diagnostics, backup/restore deduplication and compatibility with older stored ranks follow the specification.

## Coding and Dependencies

- Target .NET 10 and its default C# 14; do not pin an older language version. Keep nullable reference types and implicit usings enabled. Prefer file-scoped namespaces.
- Use four-space indentation and C# braces on separate lines. Follow existing project style where the coding standard allows it.
- Use PascalCase for types, methods, properties, events, constants and static readonly fields; camelCase for parameters and locals; `_camelCase` for private instance fields. Avoid `this.`. Prefix interfaces with `I` and suffix async methods with `Async`.
- Use PascalCase components with matching `.razor.css` files and shared imports in `_Imports.razor`.
- Prefer immutable records/value objects, primary constructors, collection expressions, pattern matching, required members and modern idioms when they improve clarity. Prefer simple focused functions and composition; add abstractions and interfaces only for a clear need.
- Services use constructor injection; Razor components use `@inject` or code-behind `[Inject]` properties. Do not use method injection.
- Keep async code async throughout. Never block with `.Result`, `.Wait()` or `.GetAwaiter().GetResult()`. Use `ValueTask` only when profiling demonstrates a benefit.
- Prefer `is null` / `is not null`, `ArgumentNullException.ThrowIfNull()` and meaningful nullability. Do not use the null-forgiving operator without a documented reason.
- Use operation-specific result records or enums for expected failures and contextual exceptions for exceptional failures. Do not add a generic result library.
- Minimise dependencies. Every package, library, tool and asset must satisfy the coding standard's OSI-approved open-source licence rule. Check the exact version's licence; never add commercial, restricted dual-licensed, source-available or sponsorware dependencies.
- Apply `.editorconfig` and StyleCop analyzers to every project. Enforce tool-checkable coding rules in configuration. Release builds and publish treat warnings as errors. Fix analyzer warnings; never suppress them to pass a build. Record deliberate repo-wide rule changes beside the rule.

## UI Requirements

- Design for portrait iPhone use, including iPhone SE at 375 × 667, without horizontal scrolling.
- Respect notch and home-indicator safe areas. Centre dialogs; keep titles, messages and all buttons reachable at every page scroll position, clear of the toolbar. Tall dialogs scroll internally.
- Make drag and press-and-hold work with touch on iOS Safari without text selection or callout menus.
- Support light, dark and remembered device-following themes from initial paint without a wrong-theme flash. Form controls follow the theme.
- Provide accessible names and semantic controls. Text contrast is at least 4.5:1 in both themes, including court numbers on every ball colour. Courtside content must be large and readable at night.

## Build, Run and Verification

From the repository root:

```sh
dotnet restore SocialTennis.slnx
dotnet build SocialTennis.slnx
dotnet build SocialTennis.slnx -c Release
dotnet test SocialTennis.slnx
dotnet format SocialTennis.slnx --verify-no-changes --severity info --no-restore
dotnet run --project app.codex.tennis --launch-profile http
python3 scripts/publish.py --base /ai.test.tennis.app.codex/
```

The `http` profile launches at `http://localhost:3000`. First download WebKit after building: `dotnet exec --runtimeconfig tests/SocialTennis.Tests/bin/Debug/net10.0/SocialTennis.Tests.runtimeconfig.json tests/SocialTennis.Tests/bin/Debug/net10.0/Microsoft.Playwright.dll install webkit`. On Linux, append `--with-deps` to install browser system dependencies. `dotnet test` automatically publishes stamped Release output, hosts it beneath `/tennis/`, and runs the browser suite. Python 3 is required for publishing and the static test server. Deployment files and screenshots go to ignored `artifacts/`.

- Every feature and bug fix requires automated tests written alongside implementation. Every task requirement needs test evidence; manual checks alone do not satisfy completion.
- Prefer NUnit constraint assertions, Arrange-Act-Assert and descriptive `Method_Scenario_Expected` names. Use Shouldly if fluent assertions are needed and NSubstitute only at true boundaries; prefer clear handwritten fakes.
- Unit-test all rules, validation, parsing, searching, sorting, storage round-trips, edge cases and failures. Tests are independent, fast and deterministic, without wall-clock dependencies or unseeded randomness.
- Test schedule rules for every supported player/court combination across several seeds. Check invariants with test code independent of engine helpers, especially no repeated partners and no player on multiple courts in a round.
- Run end-to-end tests against published Release output with offline support active, using WebKit by default and iPhone viewports including iPhone SE. Start each test with empty storage; interact through visible text, labels and roles, using real drag and press-and-hold gestures.
- For every task, cover main journeys, persistence after closing/reopening, offline operation with the server actually unreachable, smallest-screen dialogs and version display after stamped publishing. Check scheduling performance in browser WebAssembly for the largest realistic session, such as 24 players on six courts.
- Require zero warnings and errors in Debug and Release for every project on disk, including publish. After repo-wide configuration changes, rebuild every project. Automate detection of IDE-only diagnostics such as naming violations as well as command-line diagnostics.

## Definition of Done and Maintaining Agent Notes

Before marking an implementation task complete:

1. Confirm every current task requirement has passing automated tests.
2. Build Debug and Release with zero warnings and errors, and verify published output as required above.
3. Run the full test suite, including browser tests.
4. Update agent notes where code changed, including this file or relevant local notes, with structure/command changes and decisions where the spec or task left a choice open.

Record every new abstraction, dependency or unspecified design choice with its reason. Keep a concise decision log and remove obsolete guidance as implementation evolves. Report unmet gates or verification limitations explicitly; do not claim completion while required work remains.

## Source Control and Deployment

- `develop` is the integration branch. Implement each task on `feature/<name>` cut from `develop` and merge back into it. Release `develop` to `master` through a pull request; there is no `main` branch.
- Every push to `master` automatically builds, tests and deploys static files to GitHub Pages under the repository subpath. Failed tests block deployment; deep links and offline operation must work there.
- Stamp each deployment automatically with version `1.0.<deploy number>`, commit and release date for About. Local builds use a fixed development version and show "Local build".
- Use short imperative commit subjects naming the task or change. Never commit build or test output. PRs explain behaviour changes, list validation and relevant issues, and include screenshots for visible UI changes.

## Decision Log

- Documentation alignment: authoritative inputs are in `app.codex.tennis/docs/`, and the app is `app.codex.tennis/app.codex.tennis.csproj`. References to `doc/` or `Codex.Tennis.App/` do not match this checkout. Existing starter structure and port are distinguished from required infrastructure so they are not treated as standards exceptions.

- Task 1 dependencies: StyleCop.Analyzers 1.2.0-beta.556 and its Unstable 1.2.0.556 implementation (MIT) support modern C# syntax; NUnit 4.5.1, NUnit3TestAdapter 6.2.0, Microsoft.NET.Test.Sdk 18.4.0 and Microsoft.Playwright 1.63.0 (all MIT) provide separate browser tests. Exact installed NuGet metadata was checked for licence compatibility. No UI framework or external artwork is needed; the tennis-ball PNGs and navigation SVGs are original.
- Task 1 shell: root redirects to Players because schedules do not exist in this increment; explicit Play navigation stays on Play. Empty Players and Schedule pages intentionally omit controls belonging to later tasks. About supplies installation guidance, themes and deployment metadata.
- Theme storage is encapsulated by the `tennisTheme` JavaScript API. It runs synchronously before CSS to prevent a wrong-theme flash, supports device changes and tolerates unavailable localStorage. Player storage is now separate from theme storage; schedule storage will be introduced with scheduling.
- Publish stages source using a canonical filesystem path (important for Razor namespaces on macOS), stamps assembly metadata and the HTML base before SDK cache hashing, then only adds uncached `404.html` and `.nojekyll`. Never rewrite cached output after publish. The worker precaches asset hashes atomically, keeps old caches until successful activation and never clears localStorage. Updates activate after successful cache installation and reload controlled pages; online reopening checks for new versions.
- Task 1 quality configuration: StyleCop is applied centrally to every project; warnings are errors in both configurations. Rule departures for file-scoped namespaces, `_camelCase`, test names and avoiding `this.` are documented beside the rules. `dotnet format --severity info` checks IDE diagnostics in CI.

- Task 1 verification: seven WebKit cases cover both 375 × 667 and 390 × 844, all navigation headings and unknown routes, installation inputs, stamped Release metadata, remembered/device/fallback themes, automatic update with storage preserved, and offline reopening after killing the server. There are no Task 1 dialogs or schedule engine to test yet. Native iPhone installation UI remains a physical-device check; automated tests verify its metadata and icon inputs. The publisher replaces only generated output after success so obsolete assets cannot remain in a deployment.

- Task 2 player management: `PlayerDetail.razor` serves `/players/add` and `/players/{id}`. `PlayerRules` keeps validation, word-based search and all four sort orders outside the UI. Names are trimmed on save; comments retain formatting. Sorting uses ordinal case-insensitive comparison for reproducible results, with the secondary name following the selected direction.
- Task 2 persistence: `IPlayerStorage` isolates browser storage; `BrowserPlayerStorage` writes one versioned JSON document at `social-tennis-players` so players and sort order save atomically. Stable GUIDs identify players for future schedules and backup deduplication. Source-generated JSON metadata supports trimmed Release builds. Loading clamps legacy ranks above 3 and defaults unknown sort values; unreadable data is reported without overwriting it. Save failures retain the form and do not navigate away. No dependency was added.
- Task 2 dialogs: native HTML `dialog` supplies modal focus containment, Escape and focus restoration in Safari; a small interop API opens/closes it. Shared styles center dialogs in the viewport, bound their height above the toolbar and allow internal scrolling. Import and backup remain Task 6 work.
- Task 2 verification: player unit tests cover validation boundaries, all ranks, word matching, sorting/ties, JSON round-trips, legacy ranks and storage failures. Published WebKit journeys cover CRUD, exact messages, all sort options, persisted comments/sort, offline CRUD after killing the server, real player preservation across a stamped update, missing players, storage errors and long-name delete dialogs in both themes on iPhone SE. The browser suite also checks theme-specific placeholder colors and touch-sized select controls. Screenshots are under ignored `artifacts/task2-*.png`.

- Task 3 structure: `Scheduling/` contains versioned session records, court parsing, seeded generation, ordering and `IScheduleStorage`; `Components/ScheduleCards.razor` shares rendering between Schedule and Play. Session storage uses a separate `social-tennis-schedule` document and source-generated JSON, keeping player storage untouched. Player snapshots keep a saved session readable if the player list later changes. Every setup transition and add/remove/reorder is saved atomically; failed writes retain the last saved state.
- Task 3 engine: bounded seeded local search swaps positions within round permutations. Incremental partner, opponent and participation counters enforce no repeat partners, doubles-first capacity and fair sitting-out/singles/three-player shares. It tries an opponent limit of two before the permitted fallback of three, prioritises fewer rank conflicts and then fewer repeated opponents, and reports failure rather than returning an invalid schedule. Four players get three rounds and five get five; larger sessions get six. No dependency was added.
- Task 3 increment boundary: review displays full names, rank dots and draggable round/game handles; Edit is visibly disabled because player editing belongs to Task 4. Play has plain numeric court labels and the Task 3 Edit link back to review, with no setup back link once in play. Task 5 replaces these with its final read-only Play controls and shortened names. No Task 3 dialogs exist. Pointer capture, touch-action and selection/callout prevention support touch dragging; arrow keys on handles provide keyboard reordering. Completed positions cannot be moved or crossed.
- Task 3 verification: 63 tests pass, including an independent matrix of 4–24 players × 1–6 courts × three seeds, deterministic reproduction, singles rank avoidance/minimum conflicts, all court errors, storage round-trips/failures and completed-position ordering restrictions. Published WebKit journeys cover both themes on iPhone SE, real handle drags, selected-player removal, bounded/excluded suggestions, setup and review persistence, backtracking to fewer courts, offline generation of 4/5/6/7-player sessions with the server stopped, offline Play reopening and schedule preservation across stamped updates. Both 24-player/six-court and ranked six-player generation finish within the five-second browser gate. Debug/Release builds and formatting verification pass without warnings; screenshots are under ignored `artifacts/task3-*.png`.
