using System;
using System.Linq;
using System.Text;
//using UnityEngine;

public static class Encryption
{
    public static string liveEncryptionPassword = "FAT COW";
    public static string EncryptDecrypt(string data, string password) {
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < data.Length; i++) {
            result.Append((char)(data[i] ^ password[i % password.Length]));
        }
        return result.ToString();
    }
    private const int SaltLength = 8; // number of random chars to add

    public static string EncryptAscii(string data, string password) {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password must not be empty.", nameof(password));

        // Generate random printable salt
        var rng = new Random();
        var salt = new string(Enumerable.Range(0, SaltLength)
            .Select(_ => (char)rng.Next(33, 126)) // printable ASCII chars
            .ToArray());

        // XOR with password+salt
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var keyBytes = Encoding.UTF8.GetBytes(password + salt);
        var xored = Xor(dataBytes, keyBytes);

        // Shuffle deterministically with password+salt
        var shuffled = Shuffle(xored, password + salt);

        // Return salt (in cleartext) + base64(shuffled data)
        return salt + Convert.ToBase64String(shuffled);
    }

    public static string DecryptAscii(string asciiCipher, string password) {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password must not be empty.", nameof(password));
        if (asciiCipher.Length < SaltLength)
            throw new ArgumentException("Ciphertext too short to contain salt.");

        // Extract salt from prefix
        var salt = asciiCipher.Substring(0, SaltLength);
        var b64 = asciiCipher.Substring(SaltLength);

        // Decode and unshuffle
        var cipherBytes = Convert.FromBase64String(b64);
        var unshuffled = Unshuffle(cipherBytes, password + salt);

        // XOR back with password+salt
        var keyBytes = Encoding.UTF8.GetBytes(password + salt);
        var plainBytes = Xor(unshuffled, keyBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    // XOR helper
    private static byte[] Xor(byte[] data, byte[] key) {
        byte[] outBytes = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            outBytes[i] = (byte)(data[i] ^ key[i % key.Length]);
        return outBytes;
    }

    // Shuffle using password+salt as RNG seed
    private static byte[] Shuffle(byte[] data, string seedKey) {
        int n = data.Length;
        var result = new byte[n];
        var indices = Permutation(n, seedKey);
        for (int i = 0; i < n; i++)
            result[i] = data[indices[i]];
        return result;
    }

    private static byte[] Unshuffle(byte[] data, string seedKey) {
        int n = data.Length;
        var result = new byte[n];
        var indices = Permutation(n, seedKey);
        for (int i = 0; i < n; i++)
            result[indices[i]] = data[i];
        return result;
    }

    // Deterministic permutation from seedKey
    private static int[] Permutation(int length, string seedKey) {
        var indices = Enumerable.Range(0, length).ToArray();
        int seed = seedKey.Aggregate(17, (h, c) => h * 31 + c); // simple hash
        var rng = new Random(seed);

        for (int i = length - 1; i > 0; i--) {
            int j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }
}
