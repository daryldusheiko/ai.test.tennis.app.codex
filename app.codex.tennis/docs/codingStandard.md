# Modern C# Coding Standard

## Target Environment
- .NET 10
- C# 14 (the .NET 10 default; don't pin an older `LangVersion`)
- Nullable reference types: **enabled**
- Implicit usings: enabled
- File-scoped namespaces: preferred

## Core Principles
- Prefer simplicity and explicitness over cleverness.
- Make illegal states unrepresentable where practical.
- Optimize for readability and long-term maintainability first.
- Only introduce abstraction when there is a clear, repeated need.
- Match the existing project style when it conflicts with these rules.

## Packages and Licences
- **Everything must be open source.** Every package, library, tool and asset uses an OSI-approved open-source licence (for example MIT, Apache 2.0 or BSD).
- Never add commercially licensed, dual-licensed (free only for some users), source-available or sponsorware packages. Some packages that used to be open source have changed licence in newer versions, so check the licence of the version you add, not just the package name.
- Keep packages to a minimum. Before adding one, check whether .NET or the existing packages already cover the need.

## Language Features (Prefer These)
- Records and record structs for DTOs, value objects, and immutable data.
- Primary constructors.
- Collection expressions (`[1, 2, 3]`, `[..existing]`).
- Pattern matching (switch expressions, property patterns, list patterns).
- `required` members.
- `raw string literals` for multi-line strings.
- `nameof` and `CallerArgumentExpression` where helpful.
- Target-typed `new()`.

Avoid older patterns unless required for compatibility:
- Avoid classic constructors + property initialization when primary constructors are clearer.
- Avoid mutable classes for simple data transfer.

## Nullability & Safety
- Nullable reference types must stay enabled.
- Don't use the null-forgiving operator (`!`) unless you have a documented reason.
- Prefer `ArgumentNullException.ThrowIfNull()` over manual checks.
- Use `is null` / `is not null` rather than `== null`.
- Prefer `string.IsNullOrWhiteSpace()` over `string.IsNullOrEmpty()` when whitespace matters.
- Return `null` only when it is a meaningful domain concept. Otherwise return an empty collection, a result type (see Error Handling) or throw.

## Async
- Async all the way. Do not block on async code (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`). This matters even more in Blazor WebAssembly, which runs on a single thread: blocking can hang the app.
- Prefer `ValueTask` only when profiling shows a benefit.

## Dependency Injection & Architecture
- Services use constructor injection. Razor components use `@inject` (or `[Inject]` properties in a code-behind), which is the Blazor convention. Don't use method injection.
- Prefer pure functions and small, focused classes.
- Keep domain logic free of infrastructure concerns: browser storage, JavaScript interop, HTTP and the file system. Domain code must be testable without a browser.
- Use interfaces only when you need substitutability or testing seams.
- Prefer composition over inheritance.

## Error Handling
- Use exceptions for exceptional cases, not for normal control flow.
- Prefer custom exception types only when callers need to handle them differently.
- For expected failures (invalid input, an edit that isn't allowed), return a small result type specific to the operation, such as a record or an enum describing the outcome. Don't add a generic `Result<T>` library.
- Always include meaningful messages and relevant context in exceptions.

## Naming & Style
- PascalCase for types, methods, properties, events, constants and static readonly fields.
- camelCase for parameters and local variables.
- Private instance fields are `_camelCase`. Don't use `this.`.
- Interfaces start with `I`.
- Async methods end with `Async`.
- Avoid abbreviations unless they are extremely well-known (`Id`, `Http`, `Db`).
- Prefer descriptive names over short ones.
- Comments explain *why*, not what the code says. Match the comment density of the surrounding code.

## Testing
- Prefer NUnit.
- Use NUnit's constraint assertions (`Assert.That(actual, Is.EqualTo(expected))`). If a fluent style is wanted, use Shouldly (BSD).
- Mock only at true boundaries, with NSubstitute (BSD). Prefer simple hand-written fakes (for example an in-memory store) where they are clearer.
- Use meaningful test names (Method_Scenario_Expected).
- Follow Arrange-Act-Assert.
- Keep tests focused and fast.

## General Rules for Agents
- Always produce code that compiles under nullable reference types.
- Prefer the most modern idiomatic solution that still matches the project.
- When multiple approaches are valid, choose the one that is simplest and most consistent with existing code.
- After writing code, build it and fix every warning (see the quality gates in `projectStandards.md`), then check it against the nullability, async and naming rules above.
- When you introduce a new abstraction or package, or make a choice the spec leaves open, record the decision and the reason in the agent notes (`CLAUDE.md`, `AGENTS.md` or equivalent).
