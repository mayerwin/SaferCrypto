using System;

namespace SaferCrypto {

    public class SaferCrypto : UnsafeCrypto {

        public override T Encrypt<T>(string plainTextMessage, string masterKey) {
            byte[] encryptedMessage;
            var encrypted = base.Encrypt<T>(plainTextMessage, masterKey);

            if (typeof(T) == typeof(string)) {
                encryptedMessage = Convert.FromBase64String(encrypted as string);
            }
            else if (typeof(T) == typeof(byte[])) {
                encryptedMessage = encrypted as byte[];
            }
            else {
                throw new SafeCryptoException(SafeCryptoException.SafeCryptoFailedReason.CastingNotSupported);
            }

            var initializationVector = new byte[SaltBitSize / 8];

            Array.Copy(encryptedMessage, 0, initializationVector, 0, initializationVector.Length);

            var authKey = GetAuthenticationKeyBytes(masterKey);
            var hmacedBytes = AppendHmacBytes(authKey, encryptedMessage);

            if (typeof(T) == typeof(string)) {
                return Convert.ToBase64String(hmacedBytes) as T;
            }

            if (typeof(T) == typeof(byte[])) {
                return hmacedBytes as T;
            }

            throw new SafeCryptoException(SafeCryptoException.SafeCryptoFailedReason.CastingNotSupported);
        }

        public override T Decrypt<T>(string encryptedInBase64, string encryptionKey) {
            var hmac = new byte[32];
            var enctyptedBytes = Convert.FromBase64String(encryptedInBase64);
            var deHmacedBytes = new byte[enctyptedBytes.Length - hmac.Length];
            var nonce = new byte[BlockBitSize / 8];

            Array.Copy(enctyptedBytes, 0, hmac, 0, hmac.Length);
            Array.Copy(enctyptedBytes, hmac.Length, deHmacedBytes, 0, enctyptedBytes.Length - hmac.Length);
            Array.Copy(deHmacedBytes, 0, nonce, 0, nonce.Length);

            var calculatedMac = GetHMacBytes(GetAuthenticationKeyBytes(Encoding.GetBytes(encryptionKey)), deHmacedBytes);

            if (!CompareHMacHashes(hmac, calculatedMac))
                throw new SafeCryptoException(SafeCryptoException.SafeCryptoFailedReason.MessageAuthenticationFailed);

            return base.Decrypt<T>(Convert.ToBase64String(deHmacedBytes), encryptionKey);
        }

        private static bool CompareHMacHashes(byte[] hmacA, byte[] hmacB) {
            var compare = 0;

            for (var i = 0; i < hmacA.Length; i++) {
                compare |= hmacA[i] ^ hmacB[i];
            }

            return compare == 0;
        }

        private static byte[] AppendHmacBytes(byte[] authKey, byte[] payload) {
            return GetSequentialBytesFromMemoryStream(GetHMacBytes(authKey, payload), payload);
        }
    }
}