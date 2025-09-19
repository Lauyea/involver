﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

namespace DataAccess.Models
{
    /// <summary>
    /// 參與、訂閱
    /// </summary>
    public class Involving
    {
        public int InvolvingID { get; set; }

        /// <summary>
        /// FK to Involver
        /// </summary>
        [Required]
        public required string InvolverID { get; set; }
        /// <summary>
        /// Reference navigation to Involver
        /// </summary>
        [ForeignKey("InvolverID")]
        [InverseProperty("Involvers")]
        public Profile? Involver { get; set; }

        [Column(TypeName = "money")]
        public int Value { get; set; }

        [Column(TypeName = "money")]
        public int MonthlyValue { get; set; }

        [Column(TypeName = "money")]
        public int TotalValue { get; set; }

        /// <summary>
        /// 最新的參與時間
        /// </summary>
        [Display(Name = "Last Time")]
        public DateTime LastTime { get; set; }

        /// <summary>
        /// FK to being involved one
        /// </summary>
        //這個不用Required，因為這是被Involve的對象，可以是NULL
        public string? ProfileID { get; set; }
        /// <summary>
        /// Reference navigation to being involved one
        /// </summary>
        [InverseProperty("Involvings")]
        [ForeignKey("ProfileID")]
        public Profile? Profile { get; set; }

        /// <summary>
        /// FK to Novel
        /// </summary>
        public int? NovelID { get; set; }
        /// <summary>
        /// Reference navigation to Novel
        /// </summary>
        public Novel? Novel { get; set; }

        /// <summary>
        /// FK to Article
        /// </summary>
        public int? ArticleID { get; set; }
        /// <summary>
        /// Reference navigation to Article
        /// </summary>
        public Article? Article { get; set; }
    }
}