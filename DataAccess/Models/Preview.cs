using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class Preview
    {
        public int Id { get; set; }

        [Required]
        [StringLength(128)]
        public required string Token { get; set; }

        [Required]
        [StringLength(256)]
        public required string Title { get; set; }

        [Required]
        public required string ContentHtml { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        public required string RevokePasswordHash { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }
    }
}
