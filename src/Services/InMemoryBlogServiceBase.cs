using BooksRHere.Models;
using System.Diagnostics.CodeAnalysis;

namespace BooksRHere.Services
{
    public abstract class InMemoryBlogServiceBase : IBlogService
    {
        protected InMemoryBlogServiceBase(IHttpContextAccessor contextAccessor) => this.ContextAccessor = contextAccessor;

        protected List<Post> Cache { get; } = new List<Post>();

        protected IHttpContextAccessor ContextAccessor { get; }

        public abstract Task<bool> DeletePost(Post post);

        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "Consumer preference.")]
        public virtual IAsyncEnumerable<string> GetCategories()
        {
            return Cache
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
            return Cache
                .Where(p => p.IsPublished || IsAdmin())
                .SelectMany(post => post.Tags)
                .Select(tag => tag.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();
        }

        public virtual Task<Post?> GetPostById(string id)
        {
            var post = Cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || !post.IsVisible() || !IsAdmin()
                ? null
                : post);
        }

        public virtual Task<Post?> GetPostBySlug(string slug)
        {
            var post = Cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || !post.IsVisible() || !IsAdmin()
                ? null
                : post);
        }

        /// <remarks>Overload for getPosts method to retrieve all posts.</remarks>
        public virtual IAsyncEnumerable<Post> GetPosts()
        {
            return Cache.Where(p => p.IsVisible() || IsAdmin()).ToAsyncEnumerable();
        }

        public virtual IAsyncEnumerable<Post> GetPosts(int count, int skip = 0)
        {
            return Cache
                .Where(p => p.IsVisible() || IsAdmin())
                .Skip(skip)
                .Take(count)
                .ToAsyncEnumerable();
        }

        public virtual IAsyncEnumerable<Post> GetPostsByCategory(string category)
        {
            var posts = from p in Cache
                        where p.IsVisible() || IsAdmin()
                        where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        public virtual IAsyncEnumerable<Post> GetPostsByTag(string tag)
        {
            var posts = from p in this.Cache
                        where p.IsVisible() || IsAdmin()
                        where p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        public abstract Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null);

        public abstract Task<bool> SavePost(Post post);

        protected bool IsAdmin() => this.ContextAccessor.HttpContext?.User?.Identity.IsAuthenticated ?? false;

        protected void SortCache() => this.Cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));

        public abstract Task SaveComments(Comment comment, string postId);
    }
}
