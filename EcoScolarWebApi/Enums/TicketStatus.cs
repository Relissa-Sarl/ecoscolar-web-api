using System.Text.Json.Serialization;

namespace EcoScolarWebApi.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketStatus
{
    Pending,
    Reviewed,
    Resolved
}
