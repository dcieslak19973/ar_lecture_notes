using System.Collections.Generic;
using System.Threading.Tasks;

public interface IStorageProvider
{
    Task SaveAsync<T>(string collection, string id, T item);
    Task<T> LoadAsync<T>(string collection, string id);
    Task<List<T>> LoadAllAsync<T>(string collection);
    Task DeleteAsync(string collection, string id);
    Task<bool> ExistsAsync(string collection, string id);
}
