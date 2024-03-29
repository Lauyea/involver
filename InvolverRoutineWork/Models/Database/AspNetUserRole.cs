﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Models.Database
{
    [Index("RoleId", Name = "IX_AspNetUserRoles_RoleId")]
    public partial class AspNetUserRole
    {
        [Key]
        public string UserId { get; set; } = null!;
        [Key]
        public string RoleId { get; set; } = null!;

        [ForeignKey("RoleId")]
        [InverseProperty("AspNetUserRoles")]
        public virtual AspNetRole Role { get; set; } = null!;
        [ForeignKey("UserId")]
        [InverseProperty("AspNetUserRoles")]
        public virtual AspNetUser User { get; set; } = null!;
    }
}