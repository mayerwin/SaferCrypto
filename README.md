# SaferCrypto
This solution is a C# .NET port of the `UnsafeCrypto` and `SaferCrypto` symmetric-key encryption PHP classes from Scott Arciszewski as published [on Stack Overflow](http://stackoverflow.com/a/30189841/541420).

`SaferCrypto` library usage:

```c#
var crypto = new SaferCrypto();

var unencrypted = "Hello World!";
var encryptionKey = "This is a really simple key. Or Not.";
var encrypted = crypto.Encrypt<string>(unencrypted, encryptionKey);
var decrypted = crypto.Decrypt<string>(encrypted, encryptionKey);

Assert.Equals(decrypted, unencrypted); // Passes!
```

The solution also includes a `SaferCryptoTest` project to play with encryption and decryption:

![Screenshot](/Screenshot.png?raw=true "Screenshot")

For reference, below is a full reproduction of Scott's answer (as of May 12 '15 at 11:42) in case it were to change, as it provides helpful documentation to understand the cryptographic functions used and some relevant security guidance.

> **Important**: Unless you have a *very* particular use-case, [do not encrypt passwords](https://paragonie.com/blog/2015/08/you-wouldnt-base64-a-password-cryptography-decoded), use a password hashing algorithm instead. When someone says they *encrypt* their passwords in a server-side application, they're either uninformed or they're describing a dangerous system design. [Safely storing passwords](https://paragonie.com/blog/2016/02/how-safely-store-password-in-2016) is a totally separate problem from encryption.
>
> Be informed. Design safe systems.
>
> # Portable Data Encryption in PHP
>
> If you're using [PHP 5.4 or newer](http://php.net/eol.php) and don't want to write a cryptography module yourself, I recommend using [an existing library that provides authenticated encryption](https://github.com/defuse/php-encryption). The library I linked relies only on what PHP provides and is under periodic review by a handful of security researchers. (Myself included.)
>
> If your portability goals do not prevent requiring PECL extensions, **[libsodium](https://pecl.php.net/package/libsodium)** is *highly* recommended over anything you or I can write in PHP.
>
> If you want to try your hand at cryptography engineering, read on.
>
> ---------------
>
> First, you should take the time to learn [the dangers of unauthenticated encryption](https://paragonie.com/blog/2015/05/using-encryption-and-authentication-correctly#title.2.1) and [the Cryptographic Doom Principle](http://www.thoughtcrime.org/blog/the-cryptographic-doom-principle/).
>
> * Encrypted data can still be tampered with by a malicious user.
> * Authenticating the encrypted data prevents tampering.
> * Authenticating the unencrypted data does not prevent tampering.
>
> ## Encryption and Decryption
>
> Encryption in PHP is actually simple (we're going to use [`openssl_encrypt()`](https://php.net/openssl_encrypt) and [`openssl_decrypt()`](https://php.net/openssl_decrypt) once you have made some decisions about how to encrypt your information. Consult `openssl_get_cipher_methods()` for a list of the methods supported on your system. The best choice is [AES in CTR mode](http://www.daemonology.net/blog/2009-06-11-cryptographic-right-answers.html):
>
> * `aes-128-ctr`
> * `aes-192-ctr`
> * `aes-256-ctr`
>
> There is currently no reason to believe that the [AES key size](https://github.com/defuse/php-encryption/issues/35) is a significant issue to worry about (bigger is probably *not* better, due to bad key-scheduling in the 256-bit mode).
>
> Note: **We are not using `mcrypt` because it is [abandonware](http://sourceforge.net/projects/mcrypt/files/Libmcrypt)** and has [unpatched bugs](http://sourceforge.net/p/mcrypt/patches/10) that might be security-affecting. Because of these reasons, I encourage other PHP developers to avoid it as well.
>
> ### Simple Encryption/Decryption Wrapper using OpenSSL
>
>     class UnsafeCrypto
>     {
>         const METHOD = 'aes-256-ctr';
>         
>         /**
>          * Encrypts (but does not authenticate) a message
>          * 
>          * @param string $message - plaintext message
>          * @param string $key - encryption key (raw binary expected)
>          * @param boolean $encode - set to TRUE to return a base64-encoded 
>          * @return string (raw binary)
>          */
>         public static function encrypt($message, $key, $encode = false)
>         {
>             $nonceSize = openssl_cipher_iv_length(self::METHOD);
>             $nonce = openssl_random_pseudo_bytes($nonceSize);
>             
>             $ciphertext = openssl_encrypt(
>                 $message,
>                 self::METHOD,
>                 $key,
>                 OPENSSL_RAW_DATA,
>                 $nonce
>             );
>             
>             // Now let's pack the IV and the ciphertext together
>             // Naively, we can just concatenate
>             if ($encode) {
>                 return base64_encode($nonce.$ciphertext);
>             }
>             return $nonce.$ciphertext;
>         }
>         
>         /**
>          * Decrypts (but does not verify) a message
>          * 
>          * @param string $message - ciphertext message
>          * @param string $key - encryption key (raw binary expected)
>          * @param boolean $encoded - are we expecting an encoded string?
>          * @return string
>          */
>         public static function decrypt($message, $key, $encoded = false)
>         {
>             if ($encoded) {
>                 $message = base64_decode($message, true);
>                 if ($message === false) {
>                     throw new Exception('Encryption failure');
>                 }
>             }
>     
>             $nonceSize = openssl_cipher_iv_length(self::METHOD);
>             $nonce = mb_substr($message, 0, $nonceSize, '8bit');
>             $ciphertext = mb_substr($message, $nonceSize, null, '8bit');
>             
>             $plaintext = openssl_decrypt(
>                 $ciphertext,
>                 self::METHOD,
>                 $key,
>                 OPENSSL_RAW_DATA,
>                 $nonce
>             );
>             
>             return $plaintext;
>         }
>     }
>
> ### Usage Example
>
>     $message = 'Ready your ammunition; we attack at dawn.';
>     $key = hex2bin('000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f');
>     
>     $encrypted = UnsafeCrypto::encrypt($message, $key);
>     $decrypted = UnsafeCrypto::decrypt($encrypted, $key);
>     
>     var_dump($encrypted, $decrypted);
>
> **Demo**: https://3v4l.org/jl7qR
>
> -----
>
> **The above simple crypto library still is not safe to use.** We need to [authenticate ciphertexts and verify them before we decrypt](http://tonyarcieri.com/all-the-crypto-code-youve-ever-written-is-probably-broken).
>
> **Note**: By default, `UnsafeCrypto::encrypt()` will return a raw binary string. Call it like this if you need to store it in a binary-safe format (base64-encoded):
>
>     $message = 'Ready your ammunition; we attack at dawn.';
>     $key = hex2bin('000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f');
>     
>     $encrypted = UnsafeCrypto::encrypt($message, $key, true);
>     $decrypted = UnsafeCrypto::decrypt($encrypted, $key, true);
>     
>     var_dump($encrypted, $decrypted);
>
> **Demo**: http://3v4l.org/f5K93
>
> ### Simple Authentication Wrapper
>
>     class SaferCrypto extends UnsafeCrypto
>     {
>         const HASH_ALGO = 'sha256';
>         
>         /**
>          * Encrypts then MACs a message
>          * 
>          * @param string $message - plaintext message
>          * @param string $key - encryption key (raw binary expected)
>          * @param boolean $encode - set to TRUE to return a base64-encoded string
>          * @return string (raw binary)
>          */
>         public static function encrypt($message, $key, $encode = false)
>         {
>             list($encKey, $authKey) = self::splitKeys($key);
>             
>             // Pass to UnsafeCrypto::encrypt
>             $ciphertext = parent::encrypt($message, $encKey);
>             
>             // Calculate a MAC of the IV and ciphertext
>             $mac = hash_hmac(self::HASH_ALGO, $ciphertext, $authKey, true);
>             
>             if ($encode) {
>                 return base64_encode($mac.$ciphertext);
>             }
>             // Prepend MAC to the ciphertext and return to caller
>             return $mac.$ciphertext;
>         }
>         
>         /**
>          * Decrypts a message (after verifying integrity)
>          * 
>          * @param string $message - ciphertext message
>          * @param string $key - encryption key (raw binary expected)
>          * @param boolean $encoded - are we expecting an encoded string?
>          * @return string (raw binary)
>          */
>         public static function decrypt($message, $key, $encoded = false)
>         {
>             list($encKey, $authKey) = self::splitKeys($key);
>             if ($encoded) {
>                 $message = base64_decode($message, true);
>                 if ($message === false) {
>                     throw new Exception('Encryption failure');
>                 }
>             }
>             
>             // Hash Size -- in case HASH_ALGO is changed
>             $hs = mb_strlen(hash(self::HASH_ALGO, '', true), '8bit');
>             $mac = mb_substr($message, 0, $hs, '8bit');
>             
>             $ciphertext = mb_substr($message, $hs, null, '8bit');
>             
>             $calculated = hash_hmac(
>                 self::HASH_ALGO,
>                 $ciphertext,
>                 $authKey,
>                 true
>             );
>             
>             if (!self::hashEquals($mac, $calculated)) {
>                 throw new Exception('Encryption failure');
>             }
>             
>             // Pass to UnsafeCrypto::decrypt
>             $plaintext = parent::decrypt($ciphertext, $encKey);
>             
>             return $plaintext;
>         }
>         
>         /**
>          * Splits a key into two separate keys; one for encryption
>          * and the other for authenticaiton
>          * 
>          * @param string $masterKey (raw binary)
>          * @return array (two raw binary strings)
>          */
>         protected static function splitKeys($masterKey)
>         {
>             // You really want to implement HKDF here instead!
>             return [
>                 hash_hmac(self::HASH_ALGO, 'ENCRYPTION', $masterKey, true),
>                 hash_hmac(self::HASH_ALGO, 'AUTHENTICATION', $masterKey, true)
>             ];
>         }
>         
>         /**
>          * Compare two strings without leaking timing information
>          * 
>          * @param string $a
>          * @param string $b
>          * @ref https://paragonie.com/b/WS1DLx6BnpsdaVQW
>          * @return boolean
>          */
>         protected static function hashEquals($a, $b)
>         {
>             if (function_exists('hash_equals')) {
>                 return hash_equals($a, $b);
>             }
>             $nonce = openssl_random_pseudo_bytes(32);
>             return hash_hmac(self::HASH_ALGO, $a, $nonce) === hash_hmac(self::HASH_ALGO, $b, $nonce);
>         }
>     }
>
> ### Usage Example
>
>     $message = 'Ready your ammunition; we attack at dawn.';
>     $key = hex2bin('000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f');
>     
>     $encrypted = SaferCrypto::encrypt($message, $key);
>     $decrypted = SaferCrypto::decrypt($encrypted, $key);
>     
>     var_dump($encrypted, $decrypted);
>
> **Demos**: [raw binary](http://3v4l.org/49Zdh), [base64-encoded](http://3v4l.org/DSO3I)
>
> -------
>
> If anyone wishes to use this `SaferCrypto` library in a production environment, or your own implementation of the same concepts, I strongly recommend reaching out to [your resident cryptographers](http://crypto.stackexchange.com) for a second opinion before you do. They'll be able tell you about mistakes that I might not even be aware of.
>
> You will be much better off using [a reputable cryptography library](https://paragonie.com/blog/2015/11/choosing-right-cryptography-library-for-your-php-project-guide).