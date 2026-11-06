using System;
using UnityEngine;

namespace LiteGame.Core.Storage
{
    /// <summary>本机键值存储（PlayerPrefs）；键自动加 LiteGame. 前缀，空键 fail-fast。</summary>
    public sealed class LocalStore
    {
        private const string KeyPrefix = "LiteGame.";

        public int GetInt(string key, int fallback = 0)
        {
            return PlayerPrefs.GetInt(Key(key), fallback);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(Key(key), value);
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            return PlayerPrefs.GetFloat(Key(key), fallback);
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(Key(key), value);
        }

        public string GetString(string key, string fallback = "")
        {
            return PlayerPrefs.GetString(Key(key), fallback);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(Key(key), value);
        }

        public bool GetBool(string key, bool fallback = false)
        {
            return PlayerPrefs.GetInt(Key(key), fallback ? 1 : 0) != 0;
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(Key(key), value ? 1 : 0);
        }

        public bool Has(string key)
        {
            return PlayerPrefs.HasKey(Key(key));
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(Key(key));
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        private static string Key(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key 不能为空", nameof(key));
            }

            return KeyPrefix + key;
        }
    }
}
