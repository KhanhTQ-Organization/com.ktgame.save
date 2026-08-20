using System;
using com.ktgame.save.core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.save.playerprefs
{
	public class PlayerPrefsStorage : IPlayerPrefsStorage
	{
		private readonly ISerializationProvider _serializationProvider;

		public PlayerPrefsStorage(ISerializationProvider serializationProvider)
		{
			_serializationProvider = serializationProvider;
		}

		public bool Exists(string key)
		{
			return PlayerPrefs.HasKey(key);
		}

		public void Save<TData>(string key, TData data)
		{
			var serializedData = _serializationProvider.Serialize(data);
			SetBytes(key, serializedData);
		}

		public async UniTask SaveAsync<TData>(string key, TData data)
		{
			var serializedData = await _serializationProvider.SerializeAsync(data);
			SetBytes(key, serializedData);
		}

		public TData Load<TData>(string key)
		{
			return _serializationProvider.Deserialize<TData>(GetBytes(key));
		}

		public object Load(string key, Type dataType)
		{
			return _serializationProvider.Deserialize(GetBytes(key), dataType);
		}

		public UniTask<TData> LoadAsync<TData>(string key)
		{
			var bytes = GetBytes(key);
			return _serializationProvider.DeserializeAsync<TData>(bytes);
		}

		public UniTask<object> LoadAsync(string key, Type dataType)
		{
			var bytes = GetBytes(key);
			return _serializationProvider.DeserializeAsync(bytes, dataType);
		}

		public void Copy(string fromKey, string toKey)
		{
			PlayerPrefs.SetString(toKey, PlayerPrefs.GetString(fromKey));
			PlayerPrefs.Save();
		}

		public bool Delete(string key)
		{
			if (Exists(key))
			{
				PlayerPrefs.DeleteKey(key);
				PlayerPrefs.Save();
				return true;
			}

			return false;
		}

		public void DeleteAll()
		{
			PlayerPrefs.DeleteAll();
			PlayerPrefs.Save();
		}

		public void SetFloat(string key, float value)
		{
			PlayerPrefs.SetFloat(key, value);
			PlayerPrefs.Save();
		}

		public float GetFloat(string key)
		{
			return PlayerPrefs.GetFloat(key);
		}

		public void SetInt(string key, int value)
		{
			PlayerPrefs.SetInt(key, value);
			PlayerPrefs.Save();
		}

		public int GetInt(string key)
		{
			return PlayerPrefs.GetInt(key);
		}

		public void SetString(string key, string value)
		{
			PlayerPrefs.SetString(key, value);
			PlayerPrefs.Save();
		}

		public string GetString(string key)
		{
			return PlayerPrefs.GetString(key);
		}

		private void SetBytes(string key, byte[] data)
		{
			SetString(key, Convert.ToBase64String(data));
		}

		private byte[] GetBytes(string key)
		{
			return Convert.FromBase64String(GetString(key));
		}
	}
}
