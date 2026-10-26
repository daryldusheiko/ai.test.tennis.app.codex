using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;
using SocialTennis.Players;
using SocialTennis.Scheduling;
using static Microsoft.Playwright.Assertions;

namespace SocialTennis.Tests;

/// <summary>
/// Verifies the published PWA in WebKit.
/// </summary>
[TestFixture]
public sealed class PwaTests
{
    private static readonly string[] RemainingPlayers = ["Bob Jones"];
    private readonly string _root = FindRoot();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private Process? _server;
    private string _url = string.Empty;

    private IBrowserContext Context => _context ?? throw new InvalidOperationException("Browser context has not started.");

    /// <summary>
    /// Verifies the published PWA in WebKit.
    /// </summary>
    [OneTimeSetUp]
    public async Task PublishAndLaunchAsync()
    {
        using Process publish = Start("python3", "scripts/publish.py", "--base", "/tennis/", "--version", "1.0.42", "--commit", "abc1234", "--date", "25 October 2026");
        await publish.WaitForExitAsync();
        Assert.That(publish.ExitCode, Is.Zero, "Release publish must pass before testing.");
        using Process upgrade = Start("python3", "scripts/publish.py", "--base", "/tennis/", "--output", "artifacts/upgrade", "--version", "1.0.43", "--commit", "def5678", "--date", "26 October 2026");
        await upgrade.WaitForExitAsync();
        Assert.That(upgrade.ExitCode, Is.Zero, "The update publish must pass before testing.");
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Webkit.LaunchAsync();
    }

    /// <summary>
    /// Verifies the published PWA in WebKit.
    /// </summary>
    [SetUp]
    public async Task StartFreshAsync()
    {
        using var portReservation = new TcpListener(IPAddress.Loopback, 0);
        portReservation.Start();
        int port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
        portReservation.Stop();
        _url = $"http://127.0.0.1:{port}/tennis/";
        _server = Start("python3", "scripts/serve.py", "--root", "artifacts/site/wwwroot", "--port", port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        using var client = new HttpClient();
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync(_url);
                response.EnsureSuccessStatusCode();
                break;
            }
            catch (HttpRequestException) when (attempt < 100)
            {
                await Task.Delay(50);
            }
        }

        _context = await (_browser ?? throw new InvalidOperationException("Browser missing.")).NewContextAsync(new()
        {
            ViewportSize = new() { Width = 375, Height = 667 },
            DeviceScaleFactor = 2,
            IsMobile = true,
            HasTouch = true,
        });
    }

    /// <summary>
    /// Verifies the published PWA in WebKit.
    /// </summary>
    [TearDown]
    public async Task StopAsync()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();
        }

        await StopServerAsync();
    }

    /// <summary>
    /// Verifies the published PWA in WebKit.
    /// </summary>
    [OneTimeTearDown]
    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    /// <summary>
    /// Verifies the published PWA in WebKit.
    /// </summary>
    [TestCase(375, 667)]
    [TestCase(390, 844)]
    public async Task Navigation_FreshInstall_ShowsPlayersAndAllFourPagesAsync(int width, int height)
    {
        IPage page = await OpenAsync();
        await page.SetViewportSizeAsync(width, height);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Players", Exact = true })).ToBeVisibleAsync();
        await Expect(page).ToHaveTitleAsync("Social Tennis");
        ILocator navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Main navigation" });
        Assert.That(await navigation.GetByRole(AriaRole.Link).AllTextContentsAsync(), Is.EqualTo(["Play", "Players", "Schedule", "About"]));
        foreach (string name in new[] { "Play", "Players", "Schedule", "About" })
        {
            await navigation.GetByRole(AriaRole.Link, new() { Name = name, Exact = true }).ClickAsync();
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true })).ToBeVisibleAsync();
            Assert.That(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"), Is.True);
            LocatorBoundingBoxResult? bounds = await navigation.BoundingBoxAsync();
            Assert.That(bounds?.Y + bounds?.Height, Is.LessThanOrEqualTo(height));
        }

        await page.ScreenshotAsync(new() { Path = Path.Combine(_root, $"artifacts/task1-{width}.png"), FullPage = true });
        Assert.That(await page.GetByText("Counter", new() { Exact = true }).CountAsync(), Is.Zero);
        Assert.That(await page.GetByText("Weather", new() { Exact = true }).CountAsync(), Is.Zero);
        await page.GotoAsync(_url + "unknown-address");
        await Expect(page.GetByText("Sorry, the content you are looking for does not exist.")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the published PWA in WebKit.
    /// </summary>
    [Test]
    public async Task Installation_PublishedAssets_ProvideIPhoneMetadataAndVersionAsync()
    {
        IPage page = await OpenAsync("about");
        await Expect(page.GetByText("Version 1.0.42 (abc1234)")).ToBeVisibleAsync();
        await Expect(page.GetByText("Released 25 October 2026")).ToBeVisibleAsync();
        Assert.That(await page.Locator("meta[name='apple-mobile-web-app-title']").GetAttributeAsync("content"), Is.EqualTo("Tennis"));
        Assert.That(await page.Locator("meta[name='apple-mobile-web-app-capable']").GetAttributeAsync("content"), Is.EqualTo("yes"));
        JsonElement manifest = await page.EvaluateAsync<JsonElement>("async () => (await fetch('manifest.webmanifest')).json()");
        Assert.That(manifest.GetProperty("name").GetString(), Is.EqualTo("Social Tennis"));
        Assert.That(manifest.GetProperty("short_name").GetString(), Is.EqualTo("Tennis"));
        Assert.That(manifest.GetProperty("display").GetString(), Is.EqualTo("standalone"));
        Assert.That(manifest.GetProperty("orientation").GetString(), Is.EqualTo("portrait"));
        foreach (string icon in new[] { "180", "192", "512" })
        {
            using var client = new HttpClient();
            byte[] bytes = await client.GetByteArrayAsync(_url + $"icons/tennis-{icon}.png");
            Assert.That(bytes.Take(8), Is.EqualTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
        }
    }

    /// <summary>
    /// Verifies the published PWA in WebKit.
    /// </summary>
    [Test]
    public async Task Offline_ServerStopped_ReopensAndKeepsThemeAsync()
    {
        IPage page = await OpenAsync("about");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync("Dark");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "Dark");
        await page.WaitForFunctionAsync("() => navigator.serviceWorker.controller !== null");
        await page.CloseAsync();
        await StopServerAsync();
        using var client = new HttpClient();
        Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync(_url));
        IPage reopened = await OpenAsync("about");
        await Expect(reopened.GetByLabel("Theme", new() { Exact = true })).ToHaveValueAsync("Dark");
        await Expect(reopened.Locator("html")).ToHaveAttributeAsync("data-theme", "Dark");
        await reopened.GetByRole(AriaRole.Link, new() { Name = "Play", Exact = true }).ClickAsync();
        await Expect(reopened.GetByText("No schedule in play.", new() { Exact = true })).ToBeVisibleAsync();
        await reopened.ReloadAsync();
        await Expect(reopened.GetByRole(AriaRole.Heading, new() { Name = "Play", Exact = true })).ToBeVisibleAsync();
        foreach (string name in new[] { "Players", "Schedule", "About" })
        {
            await reopened.GetByRole(AriaRole.Link, new() { Name = name, Exact = true }).ClickAsync();
            await Expect(reopened.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true })).ToBeVisibleAsync();
        }

        await reopened.ScreenshotAsync(new() { Path = Path.Combine(_root, "artifacts/task1-dark-iphone-se.png"), FullPage = true });
    }

    /// <summary>
    /// Verifies the published PWA in WebKit.
    /// </summary>
    [Test]
    public async Task Theme_DevicePreference_UpdatesUntilAnExplicitChoiceAsync()
    {
        IPage page = await OpenAsync("about");
        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "Dark");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync("Light");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "Light");
        await page.ReloadAsync();
        await Expect(page.GetByLabel("Theme", new() { Exact = true })).ToHaveValueAsync("Light");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync("Device");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "Dark");
    }

    /// <summary>
    /// Verifies a new worker activates after reopening and preserves device data.
    /// </summary>
    [Test]
    public async Task Update_NewPublishedVersion_PreservesStorageAndWorksOfflineAsync()
    {
        IPage page = await OpenAsync("about");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync("Dark");
        await page.GetByRole(AriaRole.Link, new() { Name = "Players", Exact = true }).ClickAsync();
        await AddPlayerAsync(page, "Upgrade", "Player");
        Player[] savedPlayers = SocialTennis.Players.PlayerDataJson.Deserialize(await page.EvaluateAsync<string>("() => localStorage.getItem('social-tennis-players')")).Players;
        Player[] sessionPlayers = [.. savedPlayers, .. ScheduleTests.Players(3)];
        var savedSession = new SocialTennis.Scheduling.Session(1, SocialTennis.Scheduling.ScheduleStep.Review, [4], sessionPlayers, SocialTennis.Scheduling.ScheduleEngine.Generate(sessionPlayers, 1, 42), true);
        savedSession = SocialTennis.Scheduling.ScheduleEditing.Pin(savedSession, new(0, savedSession.Rounds[0].Games[0].Left[0])) with { Editing = true };
        string scheduleJson = SocialTennis.Scheduling.SessionJson.Serialize(savedSession);
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-schedule', json)", scheduleJson);
        await page.WaitForFunctionAsync("() => navigator.serviceWorker.controller !== null");
        string previousCache = await page.EvaluateAsync<string>("async () => (await caches.keys()).find(key => key.startsWith('social-tennis-'))");
        await StopServerAsync();
        int port = new Uri(_url).Port;
        _server = Start("python3", "scripts/serve.py", "--root", "artifacts/upgrade/wwwroot", "--port", port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        using var client = new HttpClient();
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync(_url);
                response.EnsureSuccessStatusCode();
                break;
            }
            catch (HttpRequestException) when (attempt < 100)
            {
                await Task.Delay(50);
            }
        }

        await page.CloseAsync();
        IPage reopened = await OpenAsync("about");
        await Expect(reopened.GetByText("Version 1.0.43 (def5678)")).ToBeVisibleAsync(new() { Timeout = 30000 });
        await reopened.WaitForFunctionAsync("async oldCache => !(await caches.keys()).includes(oldCache)", previousCache);
        await Expect(reopened.GetByLabel("Theme", new() { Exact = true })).ToHaveValueAsync("Dark");
        await reopened.GetByRole(AriaRole.Link, new() { Name = "Players", Exact = true }).ClickAsync();
        await Expect(reopened.GetByRole(AriaRole.Link, new() { Name = "Edit Upgrade Player", Exact = true })).ToBeVisibleAsync();
        await reopened.GetByRole(AriaRole.Link, new() { Name = "About", Exact = true }).ClickAsync();
        await StopServerAsync();
        await reopened.ReloadAsync();
        await Expect(reopened.GetByText("Version 1.0.43 (def5678)")).ToBeVisibleAsync();
        await reopened.GetByRole(AriaRole.Link, new() { Name = "Play", Exact = true }).ClickAsync();
        await Expect(reopened.GetByText("Upgrade Player", new() { Exact = true }).First).ToBeVisibleAsync();
        Assert.That(await reopened.EvaluateAsync<string>("() => localStorage.getItem('social-tennis-schedule')"), Is.EqualTo(scheduleJson));
    }

    /// <summary>
    /// Verifies unavailable storage still permits navigation and theme changes.
    /// </summary>
    [Test]
    public async Task Theme_StorageUnavailable_UsesDeviceAndAllowsSessionChoiceAsync()
    {
        await Context.AddInitScriptAsync("Object.defineProperty(window, 'localStorage', { get() { throw new DOMException('Unavailable', 'SecurityError'); } });");
        IPage page = await OpenAsync("about");
        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "Dark");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync("Light");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "Light");
    }

    /// <summary>Verifies adding, validation, searching, sorting, editing and deletion.</summary>
    [Test]
    public async Task Players_MainJourney_ValidatesEditsSearchesSortsAndDeletesAsync()
    {
        IPage page = await OpenAsync("players");
        await Expect(page.GetByText("No players yet. Add your first player.")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Add player", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Expect(page.GetByText("First name is required.")).ToBeVisibleAsync();
        await page.GetByLabel("First name", new() { Exact = true }).FillAsync(new string('a', 51));
        await page.GetByLabel("Last name", new() { Exact = true }).FillAsync(new string('b', 51));
        await page.GetByLabel("Comment", new() { Exact = true }).FillAsync(new string('c', 501));
        await page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Expect(page.GetByText("First name must be 50 characters or fewer.")).ToBeVisibleAsync();
        await Expect(page.GetByText("Last name must be 50 characters or fewer.")).ToBeVisibleAsync();
        await Expect(page.GetByText("Comment must be 500 characters or fewer.")).ToBeVisibleAsync();
        await page.GetByLabel("First name", new() { Exact = true }).FillAsync("Ann");
        await page.GetByLabel("Last name", new() { Exact = true }).FillAsync("Smith");
        await page.GetByLabel("Rank", new() { Exact = true }).SelectOptionAsync("2");
        await page.GetByLabel("Comment", new() { Exact = true }).FillAsync("Left handed\nPrefers doubles");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await AddPlayerAsync(page, "Bob", "Jones");
        await page.GetByLabel("Search players", new() { Exact = true }).FillAsync("ANN sm");
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Edit Ann Smith", Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Edit Bob Jones", Exact = true })).ToHaveCountAsync(0);
        await page.GetByLabel("Search players", new() { Exact = true }).FillAsync("nobody");
        await Expect(page.GetByText("No players match “nobody”.")).ToBeVisibleAsync();
        await page.GetByLabel("Search players", new() { Exact = true }).FillAsync(string.Empty);
        foreach ((string option, string[] names) in new[]
        {
            ("FirstNameAscending", new[] { "Ann Smith", "Bob Jones" }),
            ("FirstNameDescending", ["Bob Jones", "Ann Smith"]),
            ("LastNameAscending", ["Bob Jones", "Ann Smith"]),
            ("LastNameDescending", ["Ann Smith", "Bob Jones"]),
        })
        {
            await page.GetByLabel("Sort players", new() { Exact = true }).SelectOptionAsync(option);
            await Expect(page.Locator(".player-list li span")).ToHaveTextAsync(names);
        }

        await page.GetByRole(AriaRole.Link, new() { Name = "Edit Ann Smith", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("Rank", new() { Exact = true })).ToHaveValueAsync("2");
        await Expect(page.GetByLabel("Comment", new() { Exact = true })).ToHaveValueAsync("Left handed\nPrefers doubles");
        await page.GetByLabel("First name", new() { Exact = true }).FillAsync("Anna");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit Anna Smith", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete player", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        await Expect(page.GetByText("Anna Smith will be removed. This can't be undone.")).ToBeVisibleAsync();
        await AssertDeleteDialogFitsAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(_root, "artifacts/task2-delete-iphone-se.png") });
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).Not.ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete player", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Players", Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Edit Anna Smith", Exact = true })).ToHaveCountAsync(0);
        await page.ReloadAsync();
        await Expect(page.Locator(".player-list li span")).ToHaveTextAsync(RemainingPlayers);
        await page.GotoAsync(_url + "players/00000000-0000-0000-0000-000000000001");
        await Expect(page.GetByText("This player no longer exists.")).ToBeVisibleAsync();
    }

    /// <summary>Verifies player persistence and all CRUD operations while the server is unreachable.</summary>
    [Test]
    public async Task Players_OfflineReopen_PreservesPlayersSortAndEditsAsync()
    {
        IPage page = await OpenAsync("players");
        await AddPlayerAsync(page, "Ann", "Smith");
        await AddPlayerAsync(page, "Bob", "Jones");
        await page.GetByLabel("Sort players", new() { Exact = true }).SelectOptionAsync("FirstNameDescending");
        await page.WaitForFunctionAsync("() => JSON.parse(localStorage.getItem('social-tennis-players')).sort === 1");
        await page.WaitForFunctionAsync("() => navigator.serviceWorker.controller !== null");
        await page.CloseAsync();
        await StopServerAsync();
        using var client = new HttpClient();
        Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync(_url));
        IPage reopened = await OpenAsync("players");
        await Expect(reopened.GetByLabel("Sort players", new() { Exact = true })).ToHaveValueAsync("FirstNameDescending");
        await Expect(reopened.Locator(".player-list li span")).ToHaveTextAsync(["Bob Jones", "Ann Smith"]);
        await AddPlayerAsync(reopened, "Cara", string.Empty);
        await reopened.GetByRole(AriaRole.Link, new() { Name = "Edit Ann Smith", Exact = true }).ClickAsync();
        await reopened.GetByLabel("Comment", new() { Exact = true }).FillAsync("Saved offline");
        await reopened.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await reopened.ReloadAsync();
        await reopened.GetByRole(AriaRole.Link, new() { Name = "Edit Ann Smith", Exact = true }).ClickAsync();
        await Expect(reopened.GetByLabel("Comment", new() { Exact = true })).ToHaveValueAsync("Saved offline");
        await reopened.GetByRole(AriaRole.Button, new() { Name = "Delete player", Exact = true }).ClickAsync();
        await reopened.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await reopened.ReloadAsync();
        await Expect(reopened.Locator(".player-list li span")).ToHaveTextAsync(["Cara", "Bob Jones"]);
        await reopened.ScreenshotAsync(new() { Path = Path.Combine(_root, "artifacts/task2-players-iphone-se.png"), FullPage = true });
    }

    /// <summary>Verifies theme, long names, legacy ranks and centered dialogs at either page scroll position.</summary>
    [TestCase("Light")]
    [TestCase("Dark")]
    public async Task Players_LongNamesAndLegacyRank_DialogFitsInBothThemesAsync(string theme)
    {
        IPage page = await OpenAsync("about");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync(theme);
        await page.GetByRole(AriaRole.Link, new() { Name = "Players", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("Search players", new() { Exact = true })).ToBeVisibleAsync();
        Assert.That(await page.GetByLabel("Sort players", new() { Exact = true }).EvaluateAsync<double>("element => element.getBoundingClientRect().height"), Is.GreaterThanOrEqualTo(44));
        Assert.That(await page.GetByLabel("Search players", new() { Exact = true }).EvaluateAsync<string>("element => getComputedStyle(element, '::placeholder').color"), Is.EqualTo(theme == "Dark" ? "rgb(180, 200, 186)" : "rgb(82, 100, 92)"));
        await page.EvaluateAsync("""
            () => localStorage.setItem('social-tennis-players', JSON.stringify({version:1, sort:0, players:[{
                id:'00000000-0000-0000-0000-000000000001',firstName:'A'.repeat(50),lastName:'B'.repeat(50),rank:9,comment:'Notes'
            }]}))
            """);
        await page.GotoAsync(_url + "players/00000000-0000-0000-0000-000000000001");
        await Expect(page.GetByLabel("Rank", new() { Exact = true })).ToHaveValueAsync("3");
        foreach (int position in new[] { 0, 9999 })
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Delete player", Exact = true }).ClickAsync();
            await page.EvaluateAsync("position => scrollTo(0, position)", position);
            await AssertDeleteDialogFitsAsync(page);
            Assert.That(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"), Is.True);
            await page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        }

        await page.ScreenshotAsync(new() { Path = Path.Combine(_root, $"artifacts/task2-edit-{theme}.png"), FullPage = true });
    }

    /// <summary>Verifies storage failures do not falsely report a successful save.</summary>
    [Test]
    public async Task Players_StorageFailures_PreservesFormAndReportsErrorsAsync()
    {
        IPage page = await OpenAsync("players");
        await page.GetByRole(AriaRole.Link, new() { Name = "Add player", Exact = true }).ClickAsync();
        await page.GetByLabel("First name", new() { Exact = true }).FillAsync("Ann");
        await page.EvaluateAsync("() => { window.testSetItem = Storage.prototype.setItem; Storage.prototype.setItem = () => { throw new DOMException('Full', 'QuotaExceededError'); }; }");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Expect(page.GetByText("Couldn't save players. Please try again.")).ToBeVisibleAsync();
        await Expect(page.GetByLabel("First name", new() { Exact = true })).ToHaveValueAsync("Ann");
        await page.EvaluateAsync("() => { window.testSetItem.call(localStorage, 'social-tennis-players', '{broken'); }");
        await page.GetByRole(AriaRole.Link, new() { Name = "‹ Players", Exact = true }).ClickAsync();
        await Expect(page.GetByText("Couldn't load players. Please try again.")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Add player", Exact = true })).ToHaveCountAsync(0);
    }

    /// <summary>Exercises setup, real dragging, persistence, and offline play on iPhone SE.</summary>
    [TestCase("Light")]
    [TestCase("Dark")]
    public async Task Schedule_CreateReorderAndPlay_PersistsOfflineAsync(string theme)
    {
        IPage page = await OpenAsync("about");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync(theme);
        await page.GetByRole(AriaRole.Link, new() { Name = "Players", Exact = true }).ClickAsync();
        foreach (string name in new[] { "Ann", "Bob", "Cara", "Dan", "Eve", "Finn", "Gail", "Hugh" })
        {
            await AddPlayerAsync(page, name, "Tennis");
        }

        await page.GetByRole(AriaRole.Link, new() { Name = "Schedule", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Create schedule", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Enter at least one court number, for example 4,5,6.");
        await page.GetByLabel("Select courts", new() { Exact = true }).FillAsync("5,4");
        await page.GetByLabel("Select courts", new() { Exact = true }).PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true })).ToBeDisabledAsync();
        foreach (string name in new[] { "Ann", "Bob", "Cara", "Dan", "Eve", "Finn", "Gail", "Hugh" })
        {
            await page.GetByLabel("Add players", new() { Exact = true }).FillAsync(name[..1]);
            await page.GetByRole(AriaRole.Button, new() { Name = name + " Tennis", Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Remove Hugh Tennis", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Playing (7)", Exact = true })).ToBeVisibleAsync();
        await page.ReloadAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Playing (7)", Exact = true })).ToBeVisibleAsync();
        await page.GetByLabel("Add players", new() { Exact = true }).FillAsync("H");
        await page.GetByRole(AriaRole.Button, new() { Name = "Hugh Tennis", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Back", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("Select courts", new() { Exact = true })).ToHaveValueAsync("4,5");
        await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review schedule", Exact = true })).ToBeVisibleAsync();
        Assert.That(await page.Locator("[data-round]").CountAsync(), Is.EqualTo(6));
        Assert.That(await page.GetByText("Court 4", new() { Exact = true }).CountAsync(), Is.Zero);
        string firstGame = await page.Locator("[data-round='0'] [data-game='0']").InnerTextAsync();
        await DragScheduleAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "Move game 1 in round 1", Exact = true }), page.Locator("[data-round='0'] [data-game='1']"));
        await Expect(page.Locator("[data-round='0'] [data-game='1']")).ToHaveTextAsync(firstGame, new() { UseInnerText = true });
        string firstRound = await page.Locator("[data-round='0'] [data-game='0']").InnerTextAsync();
        await DragScheduleAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "Move round 1", Exact = true }), page.Locator("[data-round='1']"));
        await Expect(page.Locator("[data-round='1'] [data-game='0']")).ToHaveTextAsync(firstRound, new() { UseInnerText = true });
        await page.ScreenshotAsync(new() { Path = Path.Combine(_root, $"artifacts/task3-review-{theme}.png"), FullPage = true });
        Assert.That(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"), Is.True);
        await page.GetByRole(AriaRole.Button, new() { Name = "Play", Exact = true }).ClickAsync();
        await Expect(page.GetByText("Court 4", new() { Exact = true }).First).ToBeVisibleAsync();
        Assert.That(await page.Locator("[data-handle]").CountAsync(), Is.Zero);
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "‹ Players", Exact = true })).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Button, new() { Name = "Play", Exact = true }).ClickAsync();
        await page.WaitForFunctionAsync("() => navigator.serviceWorker.controller !== null");
        await page.CloseAsync();
        await StopServerAsync();
        using var client = new HttpClient();
        Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync(_url));
        IPage reopened = await OpenAsync();
        await Expect(reopened.GetByRole(AriaRole.Heading, new() { Name = "Play", Exact = true })).ToBeVisibleAsync();
        await Expect(reopened.Locator("[data-round='1'] [data-game='0']")).ToContainTextAsync(firstRound.Replace("||", string.Empty).Trim(), new() { UseInnerText = true });
        await reopened.ScreenshotAsync(new() { Path = Path.Combine(_root, $"artifacts/task3-play-{theme}.png"), FullPage = true });
    }

    /// <summary>Checks WebAssembly performance using a full-sized session.</summary>
    [TestCase(24, false)]
    [TestCase(6, true)]
    public async Task Schedule_LargeSession_GeneratesInBrowserAsync(int count, bool ranked)
    {
        IPage page = await OpenAsync("schedule");
        Player[] players = [.. ScheduleTests.Players(count).Select((player, index) => player with { Rank = ranked ? (index < count / 2 ? 1 : 3) : null })];
        var session = new SocialTennis.Scheduling.Session(1, SocialTennis.Scheduling.ScheduleStep.Players, [1, 2, 3, 4, 5, 6], players, [], false);
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-schedule', json)", SocialTennis.Scheduling.SessionJson.Serialize(session));
        await page.ReloadAsync();
        var timer = Stopwatch.StartNew();
        await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review schedule", Exact = true })).ToBeVisibleAsync(new() { Timeout = 5000 });
        Assert.That(timer.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
        Assert.That(await page.Locator("[data-game]").CountAsync(), Is.EqualTo(count == 24 ? 36 : 12));
    }

    /// <summary>Creates small sessions with no server and checks each leftover type.</summary>
    [TestCase(4, 3, 0, 0)]
    [TestCase(5, 5, 0, 1)]
    [TestCase(6, 6, 1, 0)]
    [TestCase(7, 6, 1, 0)]
    public async Task Schedule_SmallSessions_CreatesOfflineAndResumesAsync(int count, int rounds, int extra, int sitting)
    {
        IPage page = await OpenAsync("schedule");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create schedule", Exact = true }).ClickAsync();
        await page.ReloadAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Courts", Exact = true })).ToBeVisibleAsync();
        await page.GetByLabel("Select courts", new() { Exact = true }).FillAsync("4,5");
        await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await Expect(page.GetByText("No players yet.", new() { Exact = false })).ToBeVisibleAsync();
        await page.GetByLabel("Add players", new() { Exact = true }).FillAsync("Missing");
        await Expect(page.GetByText("No matching players.", new() { Exact = true })).ToBeVisibleAsync();
        Player[] players = ScheduleTests.Players(count);
        var session = new SocialTennis.Scheduling.Session(1, SocialTennis.Scheduling.ScheduleStep.Players, [4, 5], players, [], false);
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-schedule', json)", SocialTennis.Scheduling.SessionJson.Serialize(session));
        await page.WaitForFunctionAsync("() => navigator.serviceWorker.controller !== null");
        await page.CloseAsync();
        await StopServerAsync();
        using var client = new HttpClient();
        Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync(_url));
        IPage reopened = await OpenAsync("schedule");
        await Expect(reopened.GetByRole(AriaRole.Heading, new() { Name = $"Playing ({count})", Exact = true })).ToBeVisibleAsync();
        await reopened.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await Expect(reopened.GetByRole(AriaRole.Heading, new() { Name = "Review schedule", Exact = true })).ToBeVisibleAsync();
        Assert.That(await reopened.Locator("[data-round]").CountAsync(), Is.EqualTo(rounds));
        Assert.That(await reopened.Locator("[data-game]").CountAsync(), Is.EqualTo(rounds * (1 + extra)));
        Assert.That(await reopened.GetByText("Sitting out:", new() { Exact = false }).CountAsync(), Is.EqualTo(rounds * sitting));
        if (rounds < 6)
        {
            await Expect(reopened.GetByText($"With {count} players only {rounds} rounds are possible without anyone partnering the same player twice.")).ToBeVisibleAsync();
        }

        await reopened.ReloadAsync();
        await Expect(reopened.GetByRole(AriaRole.Heading, new() { Name = "Review schedule", Exact = true })).ToBeVisibleAsync();
        await reopened.GetByRole(AriaRole.Button, new() { Name = "Play", Exact = true }).ClickAsync();
        await Expect(reopened.GetByText("Court 4", new() { Exact = true }).First).ToBeVisibleAsync();
    }

    /// <summary>Checks bounded suggestions, excluded selections and storage failures.</summary>
    [Test]
    public async Task Schedule_SearchAndStorageFailures_LeavesSavedSessionIntactAsync()
    {
        IPage page = await OpenAsync("schedule");
        Player[] players = ScheduleTests.Players(10);
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-players', json)", SocialTennis.Players.PlayerDataJson.Serialize(new(1, players, SocialTennis.Players.PlayerSort.FirstNameAscending)));
        await page.ReloadAsync();
        await page.EvaluateAsync("""
            () => {
                const save = Storage.prototype.setItem;
                Storage.prototype.setItem = function(key, value) {
                    if (window.failScheduleSave && key === 'social-tennis-schedule') throw new DOMException('Full', 'QuotaExceededError');
                    return save.call(this, key, value);
                };
            }
            """);
        await page.GetByRole(AriaRole.Button, new() { Name = "Create schedule", Exact = true }).ClickAsync();
        await page.GetByLabel("Select courts", new() { Exact = true }).FillAsync("1");
        await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await page.GetByLabel("Add players", new() { Exact = true }).FillAsync("p");
        Assert.That(await page.Locator(".suggestions button").CountAsync(), Is.EqualTo(8));
        await page.GetByRole(AriaRole.Button, new() { Name = "Player1 Tennis", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        await page.GetByLabel("Add players", new() { Exact = true }).FillAsync("ten player1");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Player1 Tennis", Exact = true })).ToHaveCountAsync(0);
        await page.GetByLabel("Add players", new() { Exact = true }).FillAsync("player2");
        await page.GetByRole(AriaRole.Button, new() { Name = "Player2 Tennis", Exact = true }).ClickAsync();
        await page.EvaluateAsync("() => { window.failScheduleSave = true; }");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Couldn't save the schedule. Please try again.");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Playing (1)", Exact = true })).ToBeVisibleAsync();
        await page.ReloadAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Playing (1)", Exact = true })).ToBeVisibleAsync();
    }

    /// <summary>Changing court capacity invalidates old rounds before the next saved step.</summary>
    [Test]
    public async Task Schedule_BackToCourtsWithFewerCourts_ReopensAndRegeneratesAsync()
    {
        IPage page = await OpenAsync("schedule");
        Player[] players = ScheduleTests.Players(8);
        var session = new SocialTennis.Scheduling.Session(1, SocialTennis.Scheduling.ScheduleStep.Review, [4, 5], players, SocialTennis.Scheduling.ScheduleEngine.Generate(players, 2, 0), false);
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-schedule', json)", SocialTennis.Scheduling.SessionJson.Serialize(session));
        await page.ReloadAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "‹ Players", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Back", Exact = true }).ClickAsync();
        await page.GetByLabel("Select courts", new() { Exact = true }).FillAsync("4");
        await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await page.ReloadAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Playing (8)", Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review schedule", Exact = true })).ToBeVisibleAsync();
        Assert.That(await page.Locator("[data-game]").CountAsync(), Is.EqualTo(6));
    }

    /// <summary>Exercises timed hold gestures, pinning, swapping and offline repair in both themes.</summary>
    [TestCase("Light")]
    [TestCase("Dark")]
    public async Task Editing_PinSwapAndRepair_PersistsOfflineAsync(string theme)
    {
        IPage page = await OpenAsync("about");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync(theme);
        Session session = ScheduleEditingTests.Create(8, 2);
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-schedule', json)", SocialTennis.Scheduling.SessionJson.Serialize(session));
        await page.GotoAsync(_url + "schedule");
        await page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Expect(page.GetByText("Press and hold a name to pin or swap a player.")).ToBeVisibleAsync();
        Assert.That(await page.Locator("[data-handle]").CountAsync(), Is.Zero);
        Guid first = session.Rounds[0].Games[0].Left[0];
        await HoldNameAsync(page, 0, first);
        await AssertEditingDialogFitsAsync(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Pin", Exact = true }).ClickAsync();
        await Expect(page.Locator($"[data-round='0'] [data-player='{first}']")).ToContainTextAsync("📌");
        await page.ReloadAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Done", Exact = true })).ToBeVisibleAsync();
        await HoldNameAsync(page, 0, first);
        await Expect(page.GetByText("Pinned in this game: re-scheduling won't move them and they can't be swapped.")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Unpin", Exact = true }).ClickAsync();
        await page.WaitForFunctionAsync("() => navigator.serviceWorker.controller !== null");
        await page.CloseAsync();
        await StopServerAsync();
        using var client = new HttpClient();
        Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync(_url));
        page = await OpenAsync("schedule");
        Session swapped = ScheduleEditingTests.Repeat(session);
        Guid source = session.Rounds[0].Games[0].Left[1];
        Guid target = swapped.Rounds[0].Games[0].Left[1];
        await HoldNameAsync(page, 0, source);
        await page.GetByRole(AriaRole.Button, new() { Name = "Swap", Exact = true }).ClickAsync();
        await page.Locator($"[data-round='1'] [data-player='{target}']").TapAsync();
        await Expect(page.GetByText("Pick a player in round 1.")).ToBeVisibleAsync();
        await page.Locator($"[data-round='0'] [data-player='{target}']").TapAsync();
        Assert.That(await page.Locator(".conflict").CountAsync(), Is.GreaterThan(0));
        await page.GetByRole(AriaRole.Button, new() { Name = "Re-schedule", Exact = true }).ClickAsync();
        await Expect(page.GetByText("Re-scheduled:", new() { Exact = false })).ToBeVisibleAsync(new() { Timeout = 5000 });
        Assert.That(await page.Locator(".conflict").CountAsync(), Is.Zero);
        Assert.That(await page.Locator(".moved").CountAsync(), Is.GreaterThan(0));
        await page.ScreenshotAsync(new() { Path = Path.Combine(_root, $"artifacts/task4-edit-{theme}.png"), FullPage = true });
        await page.GetByRole(AriaRole.Button, new() { Name = "Done", Exact = true }).ClickAsync();
        Assert.That(await page.Locator(".moved").CountAsync(), Is.Zero);
        Assert.That(await page.Locator("[data-handle]").CountAsync(), Is.GreaterThan(0));
        await page.ReloadAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await HoldNameAsync(page, 0, first);
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Done", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "‹ Players", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Remove Player1 Tennis", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Playing (7)", Exact = true })).ToBeVisibleAsync();
    }

    /// <summary>Checks completed-conflict choices and completed-player options at the bottom of the page.</summary>
    [TestCase(false, "Light")]
    [TestCase(true, "Dark")]
    public async Task Editing_CompletedConflict_OffersMoveOrKeepAsync(bool keep, string theme)
    {
        IPage page = await OpenAsync("about");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync(theme);
        Session session = ScheduleEditingTests.Repeat(ScheduleEditingTests.Create(8, 2)) with { Editing = true };
        session = session with { Players = [.. session.Players.Select(player => player with { FirstName = player.FirstName + new string('A', 40), LastName = new string('B', 50) })] };
        Guid[] pair = session.Rounds[0].Games.SelectMany(game => new[] { game.Left, game.Right })
            .First(side => SocialTennis.Scheduling.ScheduleEditing.Repeated(session, 0, side[0]));
        int other = Array.FindIndex(session.Rounds, 1, round => round.Games.Any(game => new[] { game.Left, game.Right }.Any(side => side.All(pair.Contains))));
        Round completed = session.Rounds[other];
        completed = keep
            ? completed with { Games = [.. completed.Games.Select(game => game.Left.Concat(game.Right).Contains(pair[0]) ? game with { Complete = true } : game)] }
            : completed with { Complete = true };
        session = SocialTennis.Scheduling.ScheduleEditing.Replace(session, other, completed);
        string completedMessage = keep ? "This game is complete, so it can't be changed." : $"Round {other + 1} is complete, so nothing in it can be changed.";
        PartnerConflict conflict = SocialTennis.Scheduling.ScheduleEditing.CompletedConflict(session) ?? throw new AssertionException("Missing conflict");
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-schedule', json)", SocialTennis.Scheduling.SessionJson.Serialize(session));
        await page.GotoAsync(_url + "schedule");
        await HoldNameAsync(page, other, pair[0]);
        await Expect(page.GetByText(completedMessage)).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button)).ToHaveCountAsync(1);
        await AssertEditingDialogFitsAsync(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Re-schedule", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Already played together", Exact = true })).ToBeVisibleAsync();
        await AssertEditingDialogFitsAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(_root, $"artifacts/task4-conflict-{keep}.png"), FullPage = false });
        if (keep)
        {
            while (SocialTennis.Scheduling.ScheduleEditing.CompletedConflict(session) is { } pending)
            {
                await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
                await page.GetByRole(AriaRole.Button, new() { Name = "Keep them together", Exact = true }).ClickAsync();
                session = SocialTennis.Scheduling.ScheduleEditing.Accept(session, pending);
            }
        }
        else
        {
            List<PartnerConflict> choices = [];
            while (SocialTennis.Scheduling.ScheduleEditing.CompletedConflict(session, [.. choices]) is { } pending)
            {
                await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
                await page.GetByRole(AriaRole.Button, new() { Name = "Move " + SocialTennis.Scheduling.ScheduleEditing.Name(session, pending.First), Exact = true }).ClickAsync();
                choices.Add(pending);
            }

            Assert.That(choices.Count, Is.GreaterThan(1));
        }

        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Re-schedule", Exact = true })).ToBeEnabledAsync();
        await page.ReloadAsync();
        await HoldNameAsync(page, other, pair[0]);
        await Expect(page.GetByText(completedMessage)).ToBeVisibleAsync();
    }

    /// <summary>Checks tall player dialogs, timing cancellation and failed writes on iPhone SE.</summary>
    [TestCase("Light")]
    [TestCase("Dark")]
    public async Task Editing_LongNamesAndStorageFailures_KeepControlsReachableAsync(string theme)
    {
        IPage page = await OpenAsync("about");
        await page.GetByLabel("Theme", new() { Exact = true }).SelectOptionAsync(theme);
        Session session = ScheduleEditingTests.Create(8, 2) with { Editing = true };
        session = session with { Players = [.. session.Players.Select(player => player with { FirstName = new string('A', 50), LastName = new string('B', 50) })] };
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-schedule', json)", SocialTennis.Scheduling.SessionJson.Serialize(session));
        await page.GotoAsync(_url + "schedule");
        Guid id = session.Rounds[5].Games[0].Left[0];
        await HoldNameAsync(page, 5, id);
        await AssertEditingDialogFitsAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(_root, $"artifacts/task4-long-dialog-{theme}.png"), FullPage = false });
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        await page.EvaluateAsync("() => { Storage.prototype.setItem = () => { throw new DOMException('Full', 'QuotaExceededError'); }; }");
        await HoldNameAsync(page, 5, id);
        await page.GetByRole(AriaRole.Button, new() { Name = "Pin", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Alert)).ToHaveTextAsync("Couldn't save the schedule. Please try again.");
        await Expect(page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Pin", Exact = true })).ToBeVisibleAsync();
        Assert.That(await page.Locator("[aria-label='Pinned']").CountAsync(), Is.Zero);
    }

    /// <summary>Checks short taps and scrolling cancel the hold without racing real timers.</summary>
    [Test]
    public async Task Editing_ShortPressAndMovement_DoNotOpenOptionsAsync()
    {
        IPage page = await OpenAsync("schedule");
        Session session = ScheduleEditingTests.Create(8, 2) with { Editing = true };
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-schedule', json)", SocialTennis.Scheduling.SessionJson.Serialize(session));
        await page.ReloadAsync();
        ILocator name = page.Locator("[data-player]").First;
        await Expect(name).ToBeVisibleAsync();
        await name.TapAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        Assert.That(await name.EvaluateAsync<string>("element => getComputedStyle(element).webkitUserSelect"), Is.EqualTo("none"));
        if (await page.EvaluateAsync<bool>("CSS.supports('-webkit-touch-callout', 'none')"))
        {
            Assert.That(await name.EvaluateAsync<string>("element => getComputedStyle(element).getPropertyValue('-webkit-touch-callout')"), Is.EqualTo("none"));
        }

        await page.Clock.InstallAsync();
        long now = await page.EvaluateAsync<long>("Date.now()");
        await page.Clock.PauseAtAsync(DateTimeOffset.FromUnixTimeMilliseconds(now + 1000).UtcDateTime);
        LocatorBoundingBoxResult box = await name.BoundingBoxAsync() ?? throw new AssertionException("Missing name");
        await page.Mouse.MoveAsync(box.X + 5, box.Y + 5);
        await page.Mouse.DownAsync();
        await page.Clock.RunForAsync(499);
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await page.Mouse.UpAsync();
        await page.Clock.RunForAsync(100);
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + 25, box.Y + 5);
        await page.Clock.RunForAsync(550);
        await page.Mouse.UpAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await page.Clock.ResumeAsync();
    }

    /// <summary>Checks repair performance in the WebAssembly runtime with six full courts.</summary>
    [Test]
    public async Task Editing_LargestSession_RepairsWithinFiveSecondsAsync()
    {
        IPage page = await OpenAsync("schedule");
        Session session = ScheduleEditingTests.Repeat(ScheduleEditingTests.Create(24, 6)) with { Editing = true };
        await page.EvaluateAsync("json => localStorage.setItem('social-tennis-schedule', json)", SocialTennis.Scheduling.SessionJson.Serialize(session));
        await page.ReloadAsync();
        var timer = Stopwatch.StartNew();
        await page.GetByRole(AriaRole.Button, new() { Name = "Re-schedule", Exact = true }).ClickAsync();
        await Expect(page.GetByText("Re-scheduled:", new() { Exact = false })).ToBeVisibleAsync(new() { Timeout = 5000 });
        Assert.That(timer.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
        Assert.That(await page.Locator(".conflict").CountAsync(), Is.Zero);
    }

    private static async Task HoldNameAsync(IPage page, int round, Guid id)
    {
        // Virtual time controls the production half-second timer; the pointer gesture is real.
        ILocator name = page.Locator($"[data-round='{round}'] [data-player='{id}']");
        await Expect(name).ToBeVisibleAsync();
        await name.ScrollIntoViewIfNeededAsync();
        await page.Clock.InstallAsync();
        long now = await page.EvaluateAsync<long>("Date.now()");
        await page.Clock.PauseAtAsync(DateTimeOffset.FromUnixTimeMilliseconds(now + 1000).UtcDateTime);
        LocatorBoundingBoxResult box = await name.BoundingBoxAsync() ?? throw new AssertionException("Missing player");
        await page.Mouse.MoveAsync(box.X + (box.Width / 2), box.Y + (box.Height / 2));
        await page.Mouse.DownAsync();
        await page.Clock.RunForAsync(550);
        await page.Mouse.UpAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        await page.Clock.ResumeAsync();
    }

    private static async Task AssertEditingDialogFitsAsync(IPage page)
    {
        ILocator dialog = page.GetByRole(AriaRole.Dialog);
        LocatorBoundingBoxResult bounds = await dialog.BoundingBoxAsync() ?? throw new AssertionException("Missing dialog");
        LocatorBoundingBoxResult toolbar = await page.GetByRole(AriaRole.Navigation).BoundingBoxAsync() ?? throw new AssertionException("Missing toolbar");
        Assert.That(bounds.Y, Is.GreaterThanOrEqualTo(0));
        Assert.That(bounds.Y + bounds.Height, Is.LessThanOrEqualTo(toolbar.Y));
        Assert.That(Math.Abs(bounds.Y + (bounds.Height / 2) - 333.5), Is.LessThan(2));
        foreach (ILocator button in await dialog.GetByRole(AriaRole.Button).AllAsync())
        {
            await button.ScrollIntoViewIfNeededAsync();
            await Expect(button).ToBeInViewportAsync();
        }

        Assert.That(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"), Is.True);
    }

    private static async Task DragScheduleAsync(IPage page, ILocator source, ILocator target)
    {
        await source.ScrollIntoViewIfNeededAsync();
        LocatorBoundingBoxResult start = await source.BoundingBoxAsync() ?? throw new AssertionException("Missing drag handle");
        await page.Mouse.MoveAsync(start.X + (start.Width / 2), start.Y + (start.Height / 2));
        await page.Mouse.DownAsync();
        await target.ScrollIntoViewIfNeededAsync();
        LocatorBoundingBoxResult end = await target.BoundingBoxAsync() ?? throw new AssertionException("Missing drag target");
        await page.Mouse.MoveAsync(end.X + (end.Width / 2), end.Y + (end.Height / 2), new() { Steps = 12 });
        await page.Mouse.UpAsync();
    }

    private static async Task AddPlayerAsync(IPage page, string firstName, string lastName)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Add player", Exact = true }).ClickAsync();
        await page.GetByLabel("First name", new() { Exact = true }).FillAsync(firstName);
        await page.GetByLabel("Last name", new() { Exact = true }).FillAsync(lastName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Players", Exact = true })).ToBeVisibleAsync();
    }

    private static async Task AssertDeleteDialogFitsAsync(IPage page)
    {
        ILocator dialog = page.GetByRole(AriaRole.Dialog);
        LocatorBoundingBoxResult bounds = await dialog.BoundingBoxAsync() ?? throw new InvalidOperationException("Dialog missing.");
        LocatorBoundingBoxResult toolbar = await page.GetByRole(AriaRole.Navigation).BoundingBoxAsync() ?? throw new InvalidOperationException("Toolbar missing.");
        Assert.That(bounds.Y, Is.GreaterThanOrEqualTo(0));
        Assert.That(bounds.Y + bounds.Height, Is.LessThanOrEqualTo(toolbar.Y));
        Assert.That(Math.Abs((bounds.Y + (bounds.Height / 2)) - 333.5), Is.LessThan(2));
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true })).ToBeInViewportAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true })).ToBeInViewportAsync();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SocialTennis.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Run tests from the repository checkout.");
    }

    private Process Start(string executable, params string[] arguments)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = _root, UseShellExecute = false };
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return Process.Start(info) ?? throw new InvalidOperationException($"Could not start {executable}.");
    }

    private async Task<IPage> OpenAsync(string route = "")
    {
        IPage page = await Context.NewPageAsync();
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                TestContext.Progress.WriteLine(message.Text);
            }
        };
        page.PageError += (_, error) => TestContext.Progress.WriteLine(error);
        await page.GotoAsync(_url + route);
        await Expect(page.GetByRole(AriaRole.Navigation)).ToBeVisibleAsync();
        return page;
    }

    private async Task StopServerAsync()
    {
        if (_server is { HasExited: false })
        {
            _server.Kill(entireProcessTree: true);
            await _server.WaitForExitAsync();
        }

        _server?.Dispose();
        _server = null;
    }
}
