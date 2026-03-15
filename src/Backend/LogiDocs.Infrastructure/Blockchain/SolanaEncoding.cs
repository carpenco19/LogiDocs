using System.Globalization;

namespace LogiDocs.Infrastructure.Blockchain;

internal static class SolanaEncoding
{
    public static byte[] GuidTo16Bytes(Guid value)
    {
        return value.ToByteArray();
    }

    public static byte[] Sha256HexTo32Bytes(string sha256Hex)
    {
        if (string.IsNullOrWhiteSpace(sha256Hex))
            throw new ArgumentException("SHA256 value is required.", nameof(sha256Hex));

        sha256Hex = sha256Hex.Trim();

        if (sha256Hex.Length != 64)
            throw new InvalidOperationException("SHA256 hex value must contain exactly 64 characters.");

        var bytes = new byte[32];

        for (int i = 0; i < 32; i++)
        {
            var hexPair = sha256Hex.Substring(i * 2, 2);
            bytes[i] = byte.Parse(hexPair, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return bytes;
    }
}