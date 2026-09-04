using System.Security.Cryptography;
using System.Text;

namespace UMonsPlanning.Pronote.Protocol;

/// <summary>
/// Reproduces <c>ObjetCryptageAES.encrypter</c> from the PRONOTE JS client.
///
/// <code>
/// encrypter(chaine, cle, iv) {
///     cle = MD5(cle)                       // cle = "" in the guest space -> MD5("")
///     iv  = iv?.length ? MD5(iv) : 16 x 0x00
///     return AES-128-CBC/PKCS7(chaine).toHex()
/// }
/// </code>
///
/// Only the order number (<c>no</c>) is encrypted: the JSON body travels in clear text (the
/// server has the "skipCryptage" option enabled on this space).
/// </summary>
public static class PronoteCrypto
{
    private static readonly byte[] ZeroIv = new byte[16];

    /// <summary>AES key: MD5 of the guest space's empty key.</summary>
    private static readonly byte[] AesKey = MD5.HashData(Array.Empty<byte>());

    /// <summary>
    /// Encrypts an order number.
    /// </summary>
    /// <param name="order">Decimal order number (1, 3, 5, ...).</param>
    /// <param name="sessionIv">
    /// Raw 16-byte IV negotiated when the session was opened, or <c>null</c> for the very first
    /// call (FonctionParametres), which uses a null IV.
    /// </param>
    public static string EncryptOrder(int order, byte[]? sessionIv)
    {
        byte[] iv = sessionIv is { Length: > 0 } ? MD5.HashData(sessionIv) : ZeroIv;

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = AesKey;
        aes.IV = iv;

        byte[] plain = Encoding.ASCII.GetBytes(order.ToString(System.Globalization.CultureInfo.InvariantCulture));
        using ICryptoTransform transform = aes.CreateEncryptor();
        byte[] cipher = transform.TransformFinalBlock(plain, 0, plain.Length);

        return Convert.ToHexString(cipher).ToLowerInvariant();
    }

    /// <summary>Generates the session IV (16 random bytes).</summary>
    public static byte[] CreateSessionIv()
    {
        byte[] iv = new byte[16];
        RandomNumberGenerator.Fill(iv);
        return iv;
    }

    /// <summary>
    /// Serializes the IV for the server (the <c>Uuid</c> field of FonctionParametres).
    /// Over HTTPS the JS client sends the IV as raw base64 ; RSA encryption is only used on a
    /// space served over plain HTTP.
    /// </summary>
    public static string SerializeIvForServer(byte[] iv) => Convert.ToBase64String(iv);
}
