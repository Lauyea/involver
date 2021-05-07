using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Involver.Models.NovelModel
{
    public class Dice
    {
        public int DiceID { get; set; }
        [Range(0,999)]
        public int Sides { get; set; }
        public int Value { get; set; }

        public int CommentID { get; set; }
        public Comment Comment { get; set; }
    }
}
