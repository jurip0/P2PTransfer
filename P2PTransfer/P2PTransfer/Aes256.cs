using P2PTransfer;
using System.Security.Cryptography;
using System.Text;

namespace Cryptography
{
    public class Aes256
    {
        private const int KeyLength = 32;
        private const int IvLength = 16;
        private readonly byte[] _key;

        private readonly byte[] Salt = {
                                70,193,115,11,17,72,44,253,177,196,251,79,31,219,51,204,177,208,47 ,
                                25,158,221,199,66,40,117,173,179,89,124,222,98
                            };
        public Aes256(string masterKey)
        {
            if (string.IsNullOrEmpty(masterKey))
                throw new ArgumentException($"{nameof(masterKey)} can not be null or empty.");

            using (var derive = new Rfc2898DeriveBytes(masterKey, Salt, 5000, HashAlgorithmName.SHA256))
            {
                _key = derive.GetBytes(KeyLength);
            }
        }
    
        public async Task EncryptStreamWithProgressBarAsync(Stream source, Stream destination, int bufferSize, long fileSize, CancellationToken cancellationToken)
        {
            using (Aes myAes = Aes.Create())
            {
                myAes.Key = _key;
                myAes.GenerateIV();

                await destination.WriteAsync(myAes.IV);

                using (var cryptoStream = new CryptoStream(destination, myAes.CreateEncryptor(myAes.Key, myAes.IV), CryptoStreamMode.Write))
                {
                    await source.CopyToAsyncWithProgressBar(cryptoStream, bufferSize, fileSize, cancellationToken);
                }
            }
        }

        public async Task DecryptStreamWithProgressBarAsync(Stream source, Stream destination, int bufferSize, long fileSize, CancellationToken cancellationToken)
        {
            using (Aes myAes = Aes.Create())
            {
                myAes.Key = _key;

                var ivBuffer = new byte[IvLength];
                await source.ReadAsync(ivBuffer, 0, ivBuffer.Length);
                myAes.IV = ivBuffer;

                using (var cryptoStream = new CryptoStream(source, myAes.CreateDecryptor(myAes.Key, myAes.IV), CryptoStreamMode.Read))
                {
                    await cryptoStream.CopyToAsyncWithProgressBar(destination, bufferSize, fileSize, cancellationToken);
                }
            }
        }

        public async Task<byte[]> EncryptStringToBytes_Aes(byte[] input)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;
                aesAlg.GenerateIV();

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    await msEncrypt.WriteAsync(aesAlg.IV, 0, aesAlg.IV.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV), CryptoStreamMode.Write))
                    {
                        await csEncrypt.WriteAsync(input, 0, input.Length);
                        await csEncrypt.FlushFinalBlockAsync();
                    }
                    return msEncrypt.ToArray();
                }
            }
        }

        public async Task<byte[]> DecryptStringFromBytes_Aes(byte[] cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;

                //set correct buffer size
                var i = 0;
                foreach (var item in cipherText)
                {
                    if (item == 0) break;
                    i++;
                }
                var newBuffer = new byte[i];
                Array.Copy(cipherText, newBuffer, i);

                using (MemoryStream msDecrypt = new MemoryStream(newBuffer))
                {
                    var ivBuffer = new byte[IvLength];
                    await msDecrypt.ReadAsync(ivBuffer, 0, IvLength);
                    aesAlg.IV = ivBuffer;

                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV), CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return Encoding.UTF8.GetBytes(await srDecrypt.ReadToEndAsync());
                        }
                    }
                }
            }
        }


    }
}
