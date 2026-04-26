using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class JsonStorageProvider : IStorageProvider
{
    private readonly string _basePath;

    public JsonStorageProvider()
    {
        _basePath = Path.Combine(Application.persistentDataPath, "data");
        Directory.CreateDirectory(_basePath);
    }

    private string GetPath(string collection, string id) =>
        Path.Combine(_basePath, collection, id + ".json");

    private string GetCollectionPath(string collection) =>
        Path.Combine(_basePath, collection);

    public Task SaveAsync<T>(string collection, string id, T item)
    {
        var dir = GetCollectionPath(collection);
        Directory.CreateDirectory(dir);
        File.WriteAllText(GetPath(collection, id), JsonUtility.ToJson(item, prettyPrint: true));
        return Task.CompletedTask;
    }

    public Task<T> LoadAsync<T>(string collection, string id)
    {
        var path = GetPath(collection, id);
        if (!File.Exists(path)) return Task.FromResult(default(T));
        return Task.FromResult(JsonUtility.FromJson<T>(File.ReadAllText(path)));
    }

    public Task<List<T>> LoadAllAsync<T>(string collection)
    {
        var dir = GetCollectionPath(collection);
        if (!Directory.Exists(dir)) return Task.FromResult(new List<T>());
        var results = new List<T>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            var item = JsonUtility.FromJson<T>(File.ReadAllText(file));
            if (item != null) results.Add(item);
        }
        return Task.FromResult(results);
    }

    public Task DeleteAsync(string collection, string id)
    {
        var path = GetPath(collection, id);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string collection, string id) =>
        Task.FromResult(File.Exists(GetPath(collection, id)));
}
