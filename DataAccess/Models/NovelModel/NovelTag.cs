using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.NovelModel
{
    /// <summary>
    /// 小說標籤。
    /// </summary>
    /// TODO: 之後可能會與Novel共用
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
