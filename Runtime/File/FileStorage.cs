using System;
using System.IO;
using com.ktgame.save.core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.save.file
{
	public class FileStorage : IFileStorage
	{
		private readonly string _path;
		private readonly ISerializationProvider _serializationProvider;

		public FileStorage(ISerializationProvider serializationProvider, string path)
		{
			_serializationProvider = serializationProvider;

			_path = string.IsNullOrEmpty(path)
				? Application.persistentDataPath
				: Path.Combine(Application.persistentDataPath, path);

			if (Directory.Exists(_path) == false)
			{
				Directory.CreateDirectory(_path);
			}
		}

		public string GetFilePath(string fileName)
		{
			return Path.Combine(_path, fileName);
		}

		public bool Exists(string key)
		{
			return File.Exists(GetFilePath(key));
		}

		public void Save<TData>(string key, TData data)
		{
			Write(_serializationProvider.Serialize(data), key);
		}

		public async UniTask SaveAsync<TData>(string key, TData data)
		{
			await _serializationProvider.SerializeAsync(data).ContinueWith(bytes => WriteAsync(bytes, key));
		}

		public TData Load<TData>(string key)
		{
			return _serializationProvider.Deserialize<TData>(Read(key));
		}

		public object Load(string key, Type dataType)
		{
			return _serializationProvider.Deserialize(Read(key), dataType);
		}

		public UniTask<TData> LoadAsync<TData>(string key)
		{
			return ReadAsync(key).ContinueWith(bytes => _serializationProvider.DeserializeAsync<TData>(bytes));
		}

		public UniTask<object> LoadAsync(string key, Type dataType)
		{
			return ReadAsync(key).ContinueWith(bytes => _serializationProvider.DeserializeAsync(bytes, dataType));
		}

		public void Copy(string fromKey, string toKey)
		{
			var fromPath = GetFilePath(fromKey);
			var toPath = GetFilePath(toKey);
			if (File.Exists(fromPath))
			{
				File.Copy(fromPath, toPath);
			}
		}

		public bool Delete(string key)
		{
			if (Exists(key))
			{
				File.Delete(GetFilePath(key));
				return true;
			}

			return false;
		}

		public void DeleteAll()
		{
			var info = new DirectoryInfo(_path);
			var files = info.GetFiles();
			for (var i = 0; i < files.Length; i++)
			{
				files[i].Delete();
			}
		}

		private void Write(byte[] output, string fileName)
		{
			File.WriteAllBytes(GetFilePath(fileName), output);
		}

		private UniTask WriteAsync(byte[] output, string fileName)
		{
			var fileStream = CreateFileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
			return fileStream.WriteAsync(output, 0, output.Length).ContinueWith(_ => fileStream.Dispose()).AsUniTask();
		}

		private byte[] Read(string fileName)
		{
			return File.ReadAllBytes(GetFilePath(fileName));
		}

		private UniTask<byte[]> ReadAsync(string fileName)
		{
			var fileStream = CreateFileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			var buffer = new byte[fileStream.Length];
			return fileStream.ReadAsync(buffer, 0, buffer.Length)
				.ContinueWith(_ =>
				{
					fileStream.Dispose();
					return buffer;
				}).AsUniTask();
		}

		private FileStream CreateFileStream(string fileName, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
		{
			return new FileStream(GetFilePath(fileName), fileMode, fileAccess, fileShare, 4096, true);
		}
	}
}
