using System.Text.Json.Serialization;

namespace SocialTennis.Players;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PlayerData))]
internal partial class PlayerJsonContext : JsonSerializerContext;
