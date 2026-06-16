using System.Security.Cryptography;

namespace EcoScolarWebApi.Helpers;

/// <summary>
/// Lightweight NanoID generator — produces URL-safe random identifiers
/// without any external dependency.
/// </summary>
internal static class NanoId
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int DefaultSize = 10;

    public static string Generate(int size = DefaultSize)
    {
        var chars = new char[size];
        var bytes = RandomNumberGenerator.GetBytes(size);

        for (var i = 0; i < size; i++)
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];

        return new string(chars);
    }
}
