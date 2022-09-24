﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Models.Database
{
    public partial class ArticleTag
    {
        public ArticleTag()
        {
            ArticleArticleTags = new HashSet<ArticleArticleTag>();
        }

        [Key]
        public int TagId { get; set; }
        [StringLength(15)]
        public string Name { get; set; } = null!;

        [InverseProperty("ArticleTagsTag")]
        public virtual ICollection<ArticleArticleTag> ArticleArticleTags { get; set; }
    }
}