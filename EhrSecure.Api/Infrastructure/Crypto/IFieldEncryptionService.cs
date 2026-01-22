namespace EhrSecure.Api.Infrastructure.Crypto;

public interface IFieldEncryptionService
{
    string EncryptToBase64(string plaintext);
    string DecryptFromBase64(string ciphertextBase64);
}
