using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AddBook.ViewModels
{
    public sealed class BookPost : IValidatableObject
    {
        private static readonly Regex HtmlTagRegex = new Regex("<[^>]*>", RegexOptions.Compiled);

        [Required]
        [RegularExpression("[0-9]{13}")]
        public string Isbn { get; set; }

        [Required]
        [RegularExpression("[0-9]{4}-[0-9]{2}-[0-9]{2}")]
        public string StartDate { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Author { get; set; }

        [Required]
        public string Editor { get; set; }

        [Required]
        public Uri CoverUrl { get; set; }

        [Required]
        public string Summary { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!DateTime.TryParse(StartDate, out _))
            {
                yield return new ValidationResult($"{nameof(StartDate)} must be a valid date", new[] { nameof(StartDate) });
            }

            if(HtmlTagRegex.IsMatch(Summary))
            {
                yield return new ValidationResult($"{nameof(Summary)} must not contain html tag", new[] { nameof(Summary) });
            }

            if (HtmlTagRegex.IsMatch(Title))
            {
                yield return new ValidationResult($"{nameof(Title)} must not contain html tag", new[] { nameof(Title) });
            }

            if (HtmlTagRegex.IsMatch(Author))
            {
                yield return new ValidationResult($"{nameof(Author)} must not contain html tag", new[] { nameof(Author) });
            }

            if (HtmlTagRegex.IsMatch(Editor))
            {
                yield return new ValidationResult($"{nameof(Editor)} must not contain html tag", new[] { nameof(Editor) });
            }
        }
    }
}