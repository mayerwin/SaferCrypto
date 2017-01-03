using System;

namespace SaferCrypto {
    public class SafeCryptoException : Exception {
        public enum SafeCryptoFailedReason {
            MessageAuthenticationFailed,
            CastingNotSupported
        }

        public SafeCryptoFailedReason Reason { get; private set; }

        public SafeCryptoException(SafeCryptoFailedReason reason) : base() {
            this.Reason = reason;
        }

        public SafeCryptoException(SafeCryptoFailedReason reason, string message) : base(message) {
            this.Reason = reason;
        }
    }
}