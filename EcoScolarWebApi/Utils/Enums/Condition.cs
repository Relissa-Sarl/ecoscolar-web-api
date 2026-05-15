using System.Runtime.Serialization;

namespace EcoscolarWebApi.Utils.Enums
{
    public enum Condition
    {
        [EnumMember(Value = "NEW")]
        NEW,
        [EnumMember(Value = "LIKE NEW")]
        LIKE_NEW,
        [EnumMember(Value = "USED")]
        USED
    }
}
