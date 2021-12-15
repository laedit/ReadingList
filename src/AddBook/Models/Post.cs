using AddBook.Business.Search.Magazine;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AddBook.Models
{
    public sealed class Post : IValidatableObject
    {
        private static readonly Regex HtmlTagRegex = new Regex("<[^>]*>", RegexOptions.Compiled);

        public PostType Type { get; set; }

        [RegularExpression("[0-9]{13}")]
        public string Isbn { get; set; }

        public MagazineName Name { get; set; }

        public string Number { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime StartDate { get; set; }

        [Required]
        public string Title { get; set; }

        public string Author { get; set; }

        public string Editor { get; set; }

        [Required]
        [DataType(DataType.ImageUrl)]
        [Display(Name = "Cover url")]
        public Uri CoverUrl { get; set; }

        [Required]
        public string Summary { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type == PostType.Book)
            {
                if (string.IsNullOrWhiteSpace(Author))
                {
                    yield return new ValidationResult($"Author is required", new[] { nameof(CoverUrl) });
                }
                else if (HtmlTagRegex.IsMatch(Author))
                {
                    yield return new ValidationResult($"{nameof(Author)} must not contain html tag", new[] { nameof(Author) });
                }

                if (string.IsNullOrWhiteSpace(Editor))
                {
                    yield return new ValidationResult($"Editor is required", new[] { nameof(CoverUrl) });
                }
                else if (HtmlTagRegex.IsMatch(Editor))
                {
                    yield return new ValidationResult($"{nameof(Editor)} must not contain html tag", new[] { nameof(Editor) });
                }
            }

            if (!Uri.IsWellFormedUriString(CoverUrl.OriginalString, UriKind.RelativeOrAbsolute))
            {
                yield return new ValidationResult($"Cover url must be a valid uri", new[] { nameof(CoverUrl) });
            }

            if (HtmlTagRegex.IsMatch(Summary))
            {
                yield return new ValidationResult($"{nameof(Summary)} must not contain html tag", new[] { nameof(Summary) });
            }

            if (HtmlTagRegex.IsMatch(Title))
            {
                yield return new ValidationResult($"{nameof(Title)} must not contain html tag", new[] { nameof(Title) });
            }
        }

        public string GetKey()
        {
            return Type switch
            {
                PostType.Book => Isbn,
                PostType.Magazine => $"{Name}-{Number.Replace(" ", "")}".ToLowerInvariant(),
                _ => throw new Exception($"Type {Type} unknown"),
            };
        }
    }
}
