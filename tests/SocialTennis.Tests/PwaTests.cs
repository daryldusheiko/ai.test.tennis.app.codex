using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace SocialTennis.Tests;

/// <summary>
/// Verifies the published PWA in WebKit.
/// </summary>
[TestFixture]
public sealed class PwaTests
{
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
        await page.EvaluateAsync("localStorage.setItem('future-player-data', 'keep me')");
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
        Assert.That(await reopened.EvaluateAsync<string>("localStorage.getItem('future-player-data')"), Is.EqualTo("keep me"));
        await StopServerAsync();
        await reopened.ReloadAsync();
        await Expect(reopened.GetByText("Version 1.0.43 (def5678)")).ToBeVisibleAsync();
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
