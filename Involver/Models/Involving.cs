using Involver.Models.ArticleModel;
using Involver.Models.NovelModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Models
{
    public class Involving
    {
        public int InvolvingID { get; set; }

        [Required]
        public string InvolverID { get; set; }
        [ForeignKey("InvolverID")]
        [InverseProperty("Involvers")]
        public Profile Involver { get; set; }

        [Column(TypeName = "money")]
        public int Value { get; set; }

        [Column(TypeName = "money")]
        public int MonthlyValue { get; set; }

        [Column(TypeName = "money")]
        public int TotalValue { get; set; }


        [Display(Name = "Last Time")]
        public DateTime LastTime { get; set; }

        //這個不用Required，因為這是被Involve的對象，可以是NULL
        public string ProfileID { get; set; }
        [InverseProperty("Involvings")]
        [ForeignKey("ProfileID")]
        public Profile Profile { get; set; }

        public int? NovelID { get; set; }
        public Novel Novel { get; set; }

        public int? ArticleID { get; set; }
        public Article Article { get; set; }
    }
}
