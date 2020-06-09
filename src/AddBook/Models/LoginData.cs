using System.ComponentModel.DataAnnotations;

namespace AddBook.Models
{
    public sealed class LoginData
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}