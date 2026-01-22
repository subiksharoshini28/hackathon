using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace EhrSecure.Api.Infrastructure.Crypto;

public sealed class AesGcmFieldEncryptionService : IFieldEncryptionService
{
    private readonly byte[] _key;

    public AesGcmFieldEncryptionService(IOptions<AesEncryptionOptions> options)
    {
        if (string.IsNullOrWhiteSpace(options.Value.AesKeyBase64))
        {
            throw new InvalidOperationException("Encryption:AesKeyBase64 is not configured.");
        }

        _key = Convert.FromBase64String(options.Value.AesKeyBase64);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Encryption key must be 32 bytes (base64 encoded) for AES-256-GCM.");
        }
    }

    public string EncryptToBase64(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var pt = Encoding.UTF8.GetBytes(plaintext);
        var ct = new byte[pt.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(_key, 16))
        {
            aes.Encrypt(nonce, pt, ct, tag);
        }

        var payload = new byte[nonce.Length + tag.Length + ct.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(ct, 0, payload, nonce.Length + tag.Length, ct.Length);

        return Convert.ToBase64String(payload);
    }

    public string DecryptFromBase64(string ciphertextBase64)
    {
        var payload = Convert.FromBase64String(ciphertextBase64);
        if (payload.Length < 12 + 16)
        {
            throw new InvalidOperationException("Ciphertext is invalid.");
        }

        var nonce = payload.AsSpan(0, 12).ToArray();
        var tag = payload.AsSpan(12, 16).ToArray();
        var ct = payload.AsSpan(28).ToArray();
        var pt = new byte[ct.Length];

        using (var aes = new AesGcm(_key, 16))
        {
            aes.Decrypt(nonce, ct, tag, pt);
        }

        return Encoding.UTF8.GetString(pt);
    }
}
