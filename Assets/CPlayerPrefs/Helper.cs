// Decompiled with JetBrains decompiler
// Type: ZKW.CryptoPlayerPrefs.Helper
// Assembly: ZKWCryptoPlayerPrefs, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A483BEBE-8FF6-452E-A100-9C2C4DD85F61
// Assembly location: D:\One Soft\Merge_Plane_Master\Merge Plane\Assets\Plugins\ZKWCryptoPlayerPrefs.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ZKW.CryptoPlayerPrefs
{
  public static class Helper
  {
    private static HashAlgorithm hash;
    private static Dictionary<string, SymmetricAlgorithm> rijndaelDict;

    public static byte[] hashBytes(byte[] input)
    {
      if (Helper.hash == null)
        Helper.hash = (HashAlgorithm) MD5.Create();
      return Helper.hash.ComputeHash(input);
    }

    private static byte[] EncryptString(byte[] clearText, SymmetricAlgorithm alg)
    {
      MemoryStream memoryStream = new MemoryStream();
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, alg.CreateEncryptor(), CryptoStreamMode.Write);
      cryptoStream.Write(clearText, 0, clearText.Length);
      cryptoStream.Close();
      return memoryStream.ToArray();
    }

    public static string EncryptString(string clearText, string Password)
    {
      SymmetricAlgorithm rijndaelForKey = Helper.getRijndaelForKey(Password);
      return Convert.ToBase64String(Helper.EncryptString(new UnicodeEncoding().GetBytes(clearText), rijndaelForKey));
    }

    private static byte[] DecryptString(byte[] cipherData, SymmetricAlgorithm alg)
    {
      MemoryStream memoryStream = new MemoryStream();
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, alg.CreateDecryptor(), CryptoStreamMode.Write);
      cryptoStream.Write(cipherData, 0, cipherData.Length);
      cryptoStream.Close();
      return memoryStream.ToArray();
    }

    public static string DecryptString(string cipherText, string Password)
    {
      if (Helper.rijndaelDict == null)
        Helper.rijndaelDict = new Dictionary<string, SymmetricAlgorithm>();
      byte[] bytes = Helper.DecryptString(Convert.FromBase64String(cipherText), Helper.getRijndaelForKey(Password));
      return new UnicodeEncoding().GetString(bytes, 0, bytes.Length);
    }

    private static SymmetricAlgorithm getRijndaelForKey(string key)
    {
      if (Helper.rijndaelDict == null)
        Helper.rijndaelDict = new Dictionary<string, SymmetricAlgorithm>();
      SymmetricAlgorithm symmetricAlgorithm;
      if (Helper.rijndaelDict.ContainsKey(key))
      {
        symmetricAlgorithm = Helper.rijndaelDict[key];
      }
      else
      {
        Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(key, new byte[13]
        {
          (byte) 73,
          (byte) 97,
          (byte) 110,
          (byte) 32,
          (byte) 77,
          (byte) 100,
          (byte) 118,
          (byte) 101,
          (byte) 101,
          (byte) 100,
          (byte) 101,
          (byte) 118,
          (byte) 118
        });
        symmetricAlgorithm = (SymmetricAlgorithm) Rijndael.Create();
        symmetricAlgorithm.Key = rfc2898DeriveBytes.GetBytes(32);
        symmetricAlgorithm.IV = rfc2898DeriveBytes.GetBytes(16);
        Helper.rijndaelDict.Add(key, symmetricAlgorithm);
      }
      return symmetricAlgorithm;
    }
  }
}
