using BooksRHere.Models;
using Couchbase.Lite;
using Couchbase.Lite.Query;
using WilderMinds.MetaWeblog;
using Post = BooksRHere.Models.Post;

namespace BooksRHere.Services
{
    public class PostDatabaseService : IDatabaseService<Post>
    {
        private readonly Database _db = Program.Db;
        private readonly IList<Post> _items;
        private readonly IDatabaseService<Comment> _commentDataStore;

        public PostDatabaseService(IDatabaseService<Comment> commentDataStore)
        {
            _commentDataStore = commentDataStore;
            _items = new List<Post>();
            var q = QueryBuilder.Select(SelectResult.Expression(Meta.ID), SelectResult.All())
                .From(DataSource.Database(_db))
                .Where(Expression.Property("Title").NotEqualTo(Expression.Value(null)))
                .OrderBy(Ordering.Property("PubDate").Descending());

            var allResult = q.Execute().AllResults();
            foreach (var res in allResult)
            {
                var p = (DictionaryObject)res[1].Value;
                var post = new Post
                {
                    ID = res[0].String,
                    Title = p.GetString("Title"),
                    Excerpt = p.GetString("Excerpt"),
                    ContentBlob = p.GetBlob("Content"),
                    Slug = p.GetString("Slug"),
                    PubDateTicks = p.GetLong("PubDate"),
                    LastModifiedTicks = p.GetLong("LastModified"),
                    IsPublished = p.GetBoolean("IsPublished"),
                };

                var cats = p.GetArray("Categories");
                var tags = p.GetArray("Tags");

                post.Categories = LoadCategories(cats);
                post.Tags = LoadTags(tags);
                post.Comments = _commentDataStore.GetItemsAsync(post.ID).Result as IList<Comment>;

                _items.Add(post);
            }
        }

        public async Task<bool> AddItemAsync(Post post)
        {
            if (post is null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            using var doc = _db.GetDocument(post.ID)?.ToMutable() ?? new MutableDocument(post.ID);

            doc.SetString("Title", post.Title);
            doc.SetString("Slug", post.Slug);
            doc.SetString("Excerpt", post.Excerpt);
            doc.SetBlob("Content", post.ContentBlob);
            doc.SetBoolean("IsPublished", post.IsPublished);
            doc.SetLong("PubDate", post.PubDateTicks);
            doc.SetLong("LastModified", post.LastModifiedTicks);
            doc.SetArray("Categories", new MutableArrayObject(post.Categories.ToArray()));
            doc.SetArray("Tags", new MutableArrayObject(post.Tags.ToArray()));

            _db.Save(doc);
            _items.Insert(0, post);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            using var doc = _db.GetDocument(id);
            if (doc != null)
                _db.Delete(doc);

            var oldItem = _items.FirstOrDefault(s => s.ID == id);
            if (oldItem != null)
            {
                _items.Remove(oldItem);
            }

            return await Task.FromResult(true);
        }

        public async Task<Post> GetItemAsync(string id)
        {
            var post = _items.FirstOrDefault(s => s.ID == id);
            post.Comments = _commentDataStore.GetItemsAsync(post.ID).Result as IList<Comment>;
            return await Task.FromResult(post);
        }

        public async Task<IEnumerable<Post>> GetItemsAsync(string id = null, bool forceRefresh = false)
        {
            if (forceRefresh) {
                foreach (var i in _items) {
                    i.Comments = await _commentDataStore.GetItemsAsync(i.ID) as IList<Comment>;
                }
            }

            return await Task.FromResult(_items);
        }

        public async Task<bool> UpdateItemAsync(Post item)
        {
            var oldItem = _items.FirstOrDefault(s => s.ID == item.ID);
            if (oldItem != null)
            {
                int index = _items.IndexOf(oldItem);

                if (index != -1)
                    _items[index] = item;
            }

            return await Task.FromResult(true);
        }

        private static IList<string> LoadCategories(ArrayObject? cats)
        {
            var categories = new List<string>();
            foreach (var c in cats)
            {
                categories.Add(c as string);
            }

            return categories;
        }

        private static IList<string> LoadTags(ArrayObject? tags)
        {
            var ts = new List<string>();
            foreach (var t in tags)
            {
                ts.Add(t as string);
            }

            return ts;
        }
    }
}
