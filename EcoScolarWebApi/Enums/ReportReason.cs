using System.Text.Json.Serialization;

namespace EcoScolarWebApi.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportReason
{
    INAPPROPRIATE_ADVERT,
    INAPPROPRIATE_COMMENT
}
