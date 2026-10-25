using Microsoft.JSInterop;

namespace SocialTennis.Players;

/// <summary>Reads and writes the device player document.</summary>
/// <param name="javaScript">Browser storage interop.</param>
public sealed class BrowserPlayerStorage(IJSRuntime javaScript) : IPlayerStorage
{
    /// <summary>Loads the saved player document.</summary>
    public async Task<PlayerData> LoadAsync()
    {
        string? json = await javaScript.InvokeAsync<string?>("localStorage.getItem", "social-tennis-players");
        return PlayerDataJson.Deserialize(json);
    }

    /// <summary>Saves the complete player document atomically.</summary>
    public async Task SaveAsync(PlayerData data)
    {
        await javaScript.InvokeVoidAsync("localStorage.setItem", "social-tennis-players", PlayerDataJson.Serialize(data));
    }
}
