using System.Collections.Generic;

namespace Involver.Models.ViewModels.Api
{
    public class CommentDto
    {
        public int CommentID { get; set; }
        public string Content { get; set; }
        public string UpdateTime { get; set; }
        public string ProfileID { get; set; }
        public string UserName { get; set; }
        public string UserImageUrl { get; set; }
        public string InvolverInfo { get; set; }
        public List<object> Dices { get; set; }
        public List<object> Messages { get; set; }
        public int AgreesCount { get; set; }
        public bool IsAgreedByCurrentUser { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanBlock { get; set; }
        public bool IsBlocked { get; set; }
    }
}
