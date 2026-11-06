using UnityEngine;

namespace LiteGame.Core.Storage
{
    /// <summary>本机键值存储（PlayerPrefs）；键自动加 LiteGame. 前缀。</summary>
    public sealed class LocalStore
    {
        private const string KeyPrefix = "LiteGame.";

        public int GetInt(string key, int fallback = 0)
        {
            return PlayerPrefs.GetInt(KeyPrefix + key, fallback);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(KeyPrefix + key, value);
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            return PlayerPrefs.GetFloat(KeyPrefix + key, fallback);
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(KeyPrefix + key, value);
        }

        public string GetString(string key, string fallback = "")
        {
            return PlayerPrefs.GetString(KeyPrefix + key, fallback);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(KeyPrefix + key, value);
        }

        public bool GetBool(string key, bool fallback = false)
        {
            return PlayerPrefs.GetInt(KeyPrefix + key, fallback ? 1 : 0) != 0;
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(KeyPrefix + key, value ? 1 : 0);
        }

        public bool Has(string key)
        {
            return PlayerPrefs.HasKey(KeyPrefix + key);
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(KeyPrefix + key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
