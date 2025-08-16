using System.Text;
using UnityEngine;

public static class Encryption
{
    public static string EncryptDecrypt(string data, string password) {
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < data.Length; i++) {
            result.Append((char)(data[i] ^ password[i % password.Length]));
        }
        return result.ToString();
    }
}
