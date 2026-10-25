using System.Text.Json.Serialization;

namespace SocialTennis.Scheduling;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Session))]
internal partial class SessionJsonContext : JsonSerializerContext;
