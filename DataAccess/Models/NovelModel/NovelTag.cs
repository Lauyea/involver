using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.NovelModel
{
    public class NovelTag
    {
        [Key]
        public int TagId { get; set; }

        [Required]
        [MaxLength(15)]
        public required string Name { get; set; }

        public ICollection<Novel>? Novels { get; set; }
    }
}
