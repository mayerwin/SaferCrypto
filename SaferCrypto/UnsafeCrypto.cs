using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace SaferCrypto {
    public class UnsafeCrypto {
        public static readonly int BlockBitSize = 128;
        public static readonly int KeyBitSize = 256;
        public static readonly int SaltBitSize = 128;
        public static readonly int Iterations = 10000;

        protected const string EncryptionKey = "ENCRYPTION";
        protected const string AuthenticationKey = "AUTHENTICATION";
        protected static Encoding Encoding = Encoding.UTF8;

        protected static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();

        public virtual T Encrypt<T>(string plainTextMessage, string masterKey) where T : class {
            var encrypt = this.Encrypt(plainTextMessage, masterKey);

            if (typeof(T) == typeof(byte[])) {
                return encrypt as T;
            }

            if (typeof(T) == typeof(string)) {
                return Convert.ToBase64String(encrypt) as T;
            }

            throw new SafeCryptoException(SafeCryptoException.SafeCryptoFailedReason.CastingNotSupported);
        }

        public virtual T Decrypt<T>(string encryptedInBase64, string masterKey) where T : class {
            var decrypted = this.Decrypt(encryptedInBase64, masterKey);

            if (typeof(T) == typeof(byte[])) {
                return decrypted as T;
            }

            if (typeof(T) == typeof(string)) {
                return Encoding.GetString(decrypted) as T;
            }

            throw new SafeCryptoException(SafeCryptoException.SafeCryptoFailedReason.CastingNotSupported);
        }

        protected static byte[] GetEncryptionKeyBytes(byte[] masterKeyBytes) {
            return GetHMacBytes(masterKeyBytes, Encoding.GetBytes(EncryptionKey));
        }

        protected static byte[] GetEncryptionKeyBytes(string masterKey) {
            return GetHMacBytes(Encoding.GetBytes(masterKey), Encoding.GetBytes(EncryptionKey));
        }

        protected static byte[] GetAuthenticationKeyBytes(string masterKey) {
            return GetHMacBytes(Encoding.GetBytes(masterKey), Encoding.GetBytes(AuthenticationKey));
        }

        protected static byte[] GetAuthenticationKeyBytes(byte[] masterKeyBytes) {
            return GetHMacBytes(masterKeyBytes, Encoding.GetBytes(AuthenticationKey));
        }

        protected static byte[] GetSequentialBytesFromMemoryStream(params byte[][] sequentialChunks) {
            using (var memoryStream = new MemoryStream()) {
                using (var binaryWriter = new BinaryWriter(memoryStream)) {
                    foreach (var chunk in sequentialChunks) {
                        binaryWriter.Write(chunk);
                    }

                    binaryWriter.Flush();
                }

                return memoryStream.ToArray();
            }
        }

        private byte[] Encrypt(string secretMessage, string masterKey) {
            var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            var inputBytes = Encoding.GetBytes(secretMessage);
            var cryptKey = GetEncryptionKeyBytes(masterKey);

            Console.WriteLine(Convert.ToBase64String(cryptKey));

            var nonce = GetNewIV();
            var cipherParameters = new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", cryptKey), nonce);

            cipher.Init(true, cipherParameters);

            var encryptedBytes = cipher.DoFinal(inputBytes);

            return GetSequentialBytesFromMemoryStream(nonce, encryptedBytes);
        }

        private byte[] Decrypt(string encryptedInBase64, string masterKey) {
            var nonce = new byte[BlockBitSize / 8];
            var encryptedMessageBytes = Convert.FromBase64String(encryptedInBase64);
            var masterKeyBytes = Encoding.UTF8.GetBytes(masterKey);
            var cryptKey = GetEncryptionKeyBytes(masterKeyBytes);

            Array.Copy(encryptedMessageBytes, 0, nonce, 0, nonce.Length);

            var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            var cipherParameters = new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", cryptKey), nonce);

            cipher.Init(false, cipherParameters);

            var encrytedText = new byte[encryptedMessageBytes.Length - nonce.Length];

            Array.Copy(encryptedMessageBytes, nonce.Length, encrytedText, 0, encrytedText.Length);

            return cipher.DoFinal(encrytedText);
        }

        private static byte[] GetNewIV() {
            var initializationVector = new byte[BlockBitSize / 8];

            Random.GetBytes(initializationVector);

            return initializationVector;
        }

        public static byte[] GetHMacBytes(byte[] authKey, byte[] payload) {
            using (var hmac = new HMACSHA256(authKey)) {
                var hash = hmac.ComputeHash(payload);

                return hash;
            }
        }
    }
}