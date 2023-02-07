using System.Diagnostics.CodeAnalysis;
using Post = BooksRHere.Models.Post;

namespace BooksRHere.Services
{
    public class FileBlogService : IBlogService
    {
        private List<Post> _cache = new List<Post>();

        private readonly IHttpContextAccessor _contextAccessor;

        private readonly IDatabaseService<Post> _postDataStore;

        [SuppressMessage(
                "Usage",
                "SecurityIntelliSenseCS:MS Security rules violation",
                Justification = "Path not derived from user input.")]
        public FileBlogService(IHttpContextAccessor contextAccessor
            , IDatabaseService<Post> postDataStore)
        {
            _postDataStore = postDataStore;
            
            this._contextAccessor = contextAccessor;
            this.LoadPosts();
        }

        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "Consumer preference.")]
        public virtual IAsyncEnumerable<string> GetCategories()
        {
            return _cache
                .Where(p => p.IsPublished || IsAdmin())
                .SelectMany(post => post.Categories)
                .Select(cat => cat.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();
        }

        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "Consumer preference.")]
        public virtual IAsyncEnumerable<string> GetTags()
        {
            return _cache
                .Where(p => p.IsPublished || IsAdmin())
                .SelectMany(post => post.Tags)
                .Select(tag => tag.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();
        }

        public virtual Task<Post?> GetPostById(string id)
        {
            var post = _cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || post.PubDate > DateTime.UtcNow || (!post.IsPublished && !IsAdmin())
                ? null
                : post);
        }

        public virtual Task<Post?> GetPostBySlug(string slug)
        {
            var post = _cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || post.PubDate > DateTime.UtcNow || (!post.IsPublished && !IsAdmin())
                ? null
                : post);
        }

        /// <remarks>Overload for getPosts method to retrieve all posts.</remarks>
        public virtual IAsyncEnumerable<Post> GetPosts()
        {
            return _cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || IsAdmin()))
                .ToAsyncEnumerable();
        }

        public virtual IAsyncEnumerable<Post> GetPosts(int count, int skip = 0)
        {
            return _cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || IsAdmin()))
                .Skip(skip)
                .Take(count)
                .ToAsyncEnumerable();
        }

        public virtual IAsyncEnumerable<Post> GetPostsByCategory(string category)
        {
            var posts = from p in _cache
                        where p.PubDate <= DateTime.UtcNow && (p.IsPublished || IsAdmin())
                        where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<Post> GetPostsByTag(string tag)
        {
            var posts = from p in _cache
                        where p.PubDate <= DateTime.UtcNow && (p.IsPublished || IsAdmin())
                        where p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        public async Task<bool> DeletePost(Post post)
        {
            return await _postDataStore.DeleteItemAsync(post.ID);
        }

        public async Task<bool> SavePost(Post post)
        {
            post.LastModified_DateTimeOffset = DateTime.UtcNow;
            return await _postDataStore.AddItemAsync(post);
        }

        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "The slug should be lower case.")]
        private async void LoadPosts()
        {
            _cache = await _postDataStore.GetItemsAsync() as List<Post>;
        }

        protected bool IsAdmin() => this._contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
    }
}
