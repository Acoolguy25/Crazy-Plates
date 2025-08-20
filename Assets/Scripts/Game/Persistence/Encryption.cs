using System;
using System.Text;
using UnityEngine;

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
    public static string EncryptAscii(string data, string password) {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password must not be empty.", nameof(password));

        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] keyBytes = Encoding.UTF8.GetBytes(password);

        byte[] xored = Xor(dataBytes, keyBytes);
        return Convert.ToBase64String(xored); // printable ASCII only
    }

    public static string DecryptAscii(string asciiCipher, string password) {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password must not be empty.", nameof(password));

        byte[] cipherBytes = Convert.FromBase64String(asciiCipher);
        byte[] keyBytes = Encoding.UTF8.GetBytes(password);

        byte[] plainBytes = Xor(cipherBytes, keyBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }
    // Helper
    private static byte[] Xor(byte[] data, byte[] key) {
        byte[] outBytes = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            outBytes[i] = (byte)(data[i] ^ key[i % key.Length]);
        return outBytes;
    }
}
