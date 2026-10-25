# Social Tennis

An offline .NET 10 Blazor WebAssembly PWA for portrait iPhone use. Task 1 provides the application shell: Play, Players, Schedule and About, installation, offline caching, themes and release metadata. Player management and scheduling arrive in later increments.

Requires .NET 10 SDK and Python 3 (only the standard library is used).

```sh
# Start development at http://localhost:3000
dotnet run --project app.codex.tennis --launch-profile http

# Build every production and test project
dotnet build SocialTennis.slnx

# First-time browser download (after building)
dotnet exec --runtimeconfig tests/SocialTennis.Tests/bin/Debug/net10.0/SocialTennis.Tests.runtimeconfig.json tests/SocialTennis.Tests/bin/Debug/net10.0/Microsoft.Playwright.dll install webkit

# All tests; automatically publishes trimmed Release files and starts/stops its server
dotnet test SocialTennis.slnx

# IDE diagnostics and formatting
dotnet format SocialTennis.slnx --verify-no-changes --severity info --no-restore

# Release quality gate
dotnet build SocialTennis.slnx -c Release

# Publish for hosting at the repository subpath
python3 scripts/publish.py --base /ai.test.tennis.app.codex/
```

The deployable static files are in `artifacts/site/wwwroot/`. Use `--base /` for a domain root. The publisher stages source, changes the base path before compilation and cache hashing, and leaves tracked files untouched. Optional `--version`, `--commit`, and `--date` arguments stamp About before trimming. Local publishing defaults to `1.0.0-dev`, `local`, and `Local build`.

To preview a published site, use `python3 scripts/serve.py --root artifacts/site/wwwroot --port 3000 --base /ai.test.tennis.app.codex/` and open that path at localhost:3000. Development intentionally does not cache assets; verify offline behaviour against published output.

On an iPhone, open the HTTPS site in Safari, then Share → Add to Home Screen. It appears as **Tennis** with a tennis-ball icon. Let the first online load finish before going offline. Later versions install online and reload the app once their complete cache is ready. Device data is stored separately from asset caches.

GitHub Actions verifies Debug, Release, IDE diagnostics and WebKit before deploying pushes to `master`. Enable GitHub Pages with **GitHub Actions** as its source in repository settings. A deployment uses `1.0.<workflow run number>`, the short commit and UTC release date. `404.html` handles initial deep links on Pages; the service worker handles subsequent online and offline navigation beneath the repository base path.

Browser tests start with empty storage, use WebKit at iPhone SE and 390 × 844 dimensions, and exercise navigation, installation metadata, stamped About, themes (including unavailable storage), reopening, online updates preserving device data, and offline operation after terminating the server. Screenshots are written to ignored `artifacts/` and uploaded by CI. Native Safari's Add to Home Screen sheet requires a physical iPhone check; browser automation verifies its manifest, icon and Apple metadata inputs.
