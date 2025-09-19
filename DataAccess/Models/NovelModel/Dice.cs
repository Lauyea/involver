using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace DataAccess.Models.NovelModel
{
    public class Dice
    {
        public int DiceID { get; set; }

        /// <summary>
        /// 面數
        /// </summary>
        [Range(0, 999)]
        public int Sides { get; set; }

        /// <summary>
        /// 總值
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// FK to Comment
        /// </summary>
        public int CommentID { get; set; }
        /// <summary>
        /// Reference navigation to Comment
        /// </summary>
        public Comment? Comment { get; set; }
    }
}