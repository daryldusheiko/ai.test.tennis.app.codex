using Microsoft.JSInterop;

namespace SocialTennis.Scheduling;

/// <summary>Reads and writes the single device session.</summary>
/// <param name="javaScript">Browser storage interop.</param>
public sealed class BrowserScheduleStorage(IJSRuntime javaScript) : IScheduleStorage
{
    /// <summary>Loads the saved session.</summary>
    public async Task<Session?> LoadAsync()
    {
        string? json = await javaScript.InvokeAsync<string?>("localStorage.getItem", "social-tennis-schedule");
        return SessionJson.Deserialize(json);
    }

    /// <summary>Saves the entire session atomically.</summary>
    public async Task SaveAsync(Session session)
    {
        await javaScript.InvokeVoidAsync("localStorage.setItem", "social-tennis-schedule", SessionJson.Serialize(session));
    }
}
