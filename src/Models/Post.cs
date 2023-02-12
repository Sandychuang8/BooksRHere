using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using BooksRHere.Objects;
using Couchbase.Lite;

namespace BooksRHere.Models
{
    public class Post
    {
        public IList<string> Categories { get; set; } = new List<string>();

        public IList<string> Tags { get; set; } = new List<string>();

        public IList<Comment> Comments { get; set; } = new List<Comment>();

        [Required]
        public string Content
        {
            get { return ContentBlob == null ? string.Empty : Encoding.UTF8.GetString(ContentBlob.Content); }
            set 
            {
                var blobContent = Encoding.UTF8.GetBytes(value);
                ContentBlob = new Blob("text/plain", blobContent);
            }
        }

        public Blob ContentBlob { get; set; }

        [Required]
        public string Excerpt { get; set; } = string.Empty;

        [Required]
        public string ID { get; set; } = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);

        public bool IsPublished { get; set; } = true;

        public long LastModifiedTicks { get; set; } = DateTime.UtcNow.Ticks;

        public DateTime LastModified => new DateTime(LastModifiedTicks);

        public long PubDateTicks { get; set; } = DateTime.UtcNow.Ticks;

        public DateTime PubDate => new DateTime(PubDateTicks);

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "The slug should be lower case.")]
        public static string CreateSlug(string title)
        {
            title = title?.ToLowerInvariant().Replace(
                Constants.Space, Constants.Dash, StringComparison.OrdinalIgnoreCase) ?? string.Empty;
            title = RemoveDiacritics(title);
            title = RemoveReservedUrlCharacters(title);

            return title.ToLowerInvariant();
        }

        public bool AreCommentsOpen(int commentsCloseAfterDays) =>
            this.PubDate.AddDays(commentsCloseAfterDays) >= DateTime.UtcNow;

        public string GetEncodedLink() => $"/blog/{System.Net.WebUtility.UrlEncode(this.Slug)}/";

        public string GetLink() => $"/blog/{this.Slug}/";

        public bool IsVisible() => this.PubDate <= DateTime.UtcNow && this.IsPublished;

        public object RenderContent()
        {
            return this.Content;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string RemoveReservedUrlCharacters(string text)
        {
            var reservedCharacters = new List<string> { "!", "#", "$", "&", "'", "(", ")", "*", ",", "/", ":", ";", "=", "?", "@", "[", "]", "\"", "%", ".", "<", ">", "\\", "^", "_", "'", "{", "}", "|", "~", "`", "+" };

            foreach (var chr in reservedCharacters)
            {
                text = text.Replace(chr, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }
    }
}
