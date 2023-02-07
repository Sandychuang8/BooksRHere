using BooksRHere.Models;

namespace BooksRHere.Services
{
    public interface IDatabaseService<T>
    {
        Task<bool> AddItemAsync(T item);
        Task<bool> UpdateItemAsync(T item);
        Task<bool> DeleteItemAsync(string id);
        Task<T> GetItemAsync(string id);
        Task<IEnumerable<T>> GetItemsAsync(string id = null, bool forceRefresh = false);
    }
}
