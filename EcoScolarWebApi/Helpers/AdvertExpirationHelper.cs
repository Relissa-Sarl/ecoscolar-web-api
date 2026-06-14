using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.Helpers;

public static class AdvertExpirationHelper
{
    public const int DefaultExpirationDays = 30;

    public static int GetExpiresInDays(DateTime createdAt)
    {
        var expirationDate = createdAt.AddDays(DefaultExpirationDays);
        var remaining = (int)(expirationDate - DateTime.UtcNow).TotalDays;
        return remaining < 0 ? 0 : remaining;
    }
}
