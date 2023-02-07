using BooksRHere.Models;

namespace BooksRHere.Services
{
    public interface IBlogService
    {
        Task<bool> DeletePost(Post post);

        Task<bool> SavePost(Post post);

        IAsyncEnumerable<string> GetCategories();

        IAsyncEnumerable<string> GetTags();

        Task<Post?> GetPostById(string id);

        Task<Post?> GetPostBySlug(string slug);

        IAsyncEnumerable<Post> GetPosts();

        IAsyncEnumerable<Post> GetPosts(int count, int skip = 0);

        IAsyncEnumerable<Post> GetPostsByCategory(string category);

        IAsyncEnumerable<Post> GetPostsByTag(string tag);
    }
}
