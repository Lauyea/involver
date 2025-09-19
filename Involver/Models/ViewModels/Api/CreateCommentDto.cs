namespace Involver.Models.ViewModels.Api
{
    public class CreateCommentDto
    {
        public string Content { get; set; }
        public string From { get; set; }
        public int FromID { get; set; }
        public int RollTimes { get; set; }
        public int DiceSides { get; set; }
    }
}