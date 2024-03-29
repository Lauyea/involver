﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Models.Database
{
    [Table("ArticleArticleTag")]
    [Index("ArticlesArticleId", Name = "IX_ArticleArticleTag_ArticlesArticleID")]
    public partial class ArticleArticleTag
    {
        [Key]
        public int ArticleTagsTagId { get; set; }
        [Key]
        [Column("ArticlesArticleID")]
        public int ArticlesArticleId { get; set; }

        [ForeignKey("ArticleTagsTagId")]
        [InverseProperty("ArticleArticleTags")]
        public virtual ArticleTag ArticleTagsTag { get; set; } = null!;
        [ForeignKey("ArticlesArticleId")]
        [InverseProperty("ArticleArticleTags")]
        public virtual Article ArticlesArticle { get; set; } = null!;
    }
}