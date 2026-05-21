using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EcoScolarWebApi.Enums;

/// <summary>
/// Enumeration that represents the status of an Adverts.
/// It can be ACTIVE, EXPIRED, SOLD or PAUSED.
/// </summary>
public enum AdvertStatus
{
    [EnumMember(Value = "ACTIVE")]
    [JsonPropertyName("ACTIVE")]
    ACTIVE,
    [EnumMember(Value = "EXPIRED")]
    [JsonPropertyName("EXPIRED")]
    EXPIRED,
    [EnumMember(Value = "SOLD")]
    [JsonPropertyName("SOLD")]
    SOLD,
    [EnumMember(Value = "PAUSED")]
    [JsonPropertyName("PAUSED")]
    PAUSED
}
