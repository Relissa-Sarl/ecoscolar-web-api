using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EcoScolarWebApi.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketStatus
{
    [EnumMember(Value = "PENDING")]
    [JsonPropertyName("PENDING")]
    PENDING,
    [EnumMember(Value = "REVIEWED")]
    [JsonPropertyName("REVIEWED")]
    REVIEWED,
    [EnumMember(Value = "RESOLVED")]
    [JsonPropertyName("RESOLVED")]
    RESOLVED
}
