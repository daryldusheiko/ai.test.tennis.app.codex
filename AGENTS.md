# Repository Guidelines

## Project Structure & Module Organization

This repository contains a single .NET 10 Blazor WebAssembly application in `Codex.Tennis.App/`. It currently includes the starter Home, Counter, Weather, and NotFound pages.

- `Program.cs` configures startup and dependency injection; `App.razor` handles routing.
- `Pages/` contains routable Razor components.
- `Layout/` contains shared layouts, navigation, and component-scoped `.razor.css` files.
- `wwwroot/` contains the HTML host, global CSS, images, and sample JSON data. Its contents are publicly accessible.
- `Properties/launchSettings.json` defines local development profiles.

There is no solution file or test project. Target the application project explicitly when running commands from the repository root.

## Build, Test, and Development Commands

Install the .NET 10 SDK. From the repository root:

```sh
dotnet restore Codex.Tennis.App/Codex.Tennis.App.csproj
dotnet build Codex.Tennis.App/Codex.Tennis.App.csproj
dotnet run --project Codex.Tennis.App --launch-profile http
dotnet watch --project Codex.Tennis.App
dotnet publish Codex.Tennis.App/Codex.Tennis.App.csproj -c Release
```

These commands restore dependencies, compile, start the HTTP development server at `http://localhost:5198`, enable live development updates, and produce release assets, respectively.

## Coding Style & Naming Conventions

Follow the existing C# and Razor style: four-space indentation, braces on separate lines in C# blocks, PascalCase for types and public members, and camelCase for locals and private fields. Name components in PascalCase, such as `MatchDetails.razor`, with matching scoped styles in `MatchDetails.razor.css`. Keep shared imports in `_Imports.razor`. Preserve nullable reference annotations; nullable checking is enabled. No repository-specific formatter or linter configuration is present.

## Testing Guidelines

No automated testing framework or coverage threshold is configured. Build before submitting changes, then manually verify affected routes and interactions in the browser. For changes involving existing samples, check the counter interaction and weather data loading. If adding tests, document the chosen framework and command, and use descriptive names such as `MatchScoreTests.cs`.

## Commit & Pull Request Guidelines

Git history currently contains only `Initial commit`, so no established commit convention exists. Use short, imperative subjects such as `Add match scoring page`. Pull requests should explain the change, link relevant issues, list validation performed, and include screenshots for visible UI changes.

## Security & Configuration

Never put credentials in client code, `wwwroot/`, or committed configuration. Blazor WebAssembly assets are delivered to the browser; keep secret-dependent operations in a server-side service.
