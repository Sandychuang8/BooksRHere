using BooksRHere.Models;
using Couchbase.Lite;
using Couchbase.Lite.Query;

namespace BooksRHere.Services
{
    public class CommentDatabaseService : IDatabaseService<Comment>
    {
        private readonly Database _db = Program.Db;
        private IList<Comment> _items;

        public async Task<bool> AddItemAsync(Comment comment)
        {
            if (comment is null)
            {
                throw new ArgumentNullException(nameof(comment));
            }

            using var doc = _db.GetDocument(comment.ID)?.ToMutable() ?? new MutableDocument(comment.ID);
            doc.SetString("Author", comment.Author);
            doc.SetString("Content", comment.Content);
            doc.SetString("Email", comment.Email);
            doc.SetString("PostID", comment.PostID);
            doc.SetBoolean("IsAdmin", comment.IsAdmin);
            doc.SetDate("PubDate", comment.PubDate_DateTimeOffset);

            _db.Save(doc);
            _items.Add(comment);

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

        public async Task<Comment> GetItemAsync(string id)
        {
            return await Task.FromResult(_items.FirstOrDefault(s => s.ID == id));
        }

        public async Task<IEnumerable<Comment>> GetItemsAsync(string postID, bool forceRefresh = false)
        {
            _items = new List<Comment>();
            var q = QueryBuilder.Select(SelectResult.Expression(Meta.ID), SelectResult.All())
                .From(DataSource.Database(_db))
                .Where(Expression.Property("PostID").EqualTo(Expression.String(postID)))
                .OrderBy(Ordering.Property("PubDate"));

            var allResult = q.Execute().AllResults();
            foreach (var res in allResult)
            {
                var c = (DictionaryObject)res[1].Value;
                var comment = new Comment
                {
                    ID = res[0].String,
                    Author = c.GetString("Author"),
                    Email = c.GetString("Email"),
                    Content = c.GetString("Content"),
                    PostID = c.GetString("PostID"),
                    PubDate_DateTimeOffset = c.GetDate("PubDate"),
                    IsAdmin = c.GetBoolean("IsAdmin"),
                };

                _items.Add(comment);
            }

            return await Task.FromResult(_items);
        }

        public async Task<bool> UpdateItemAsync(Comment item)
        {
            var oldItem = _items.FirstOrDefault(s => s.ID == item.ID);
            if (oldItem != null)
            {
                _items.Remove(oldItem);
                await DeleteItemAsync(item.ID);
                await AddItemAsync(item);
            }

            return await Task.FromResult(true);
        }
    }
}
