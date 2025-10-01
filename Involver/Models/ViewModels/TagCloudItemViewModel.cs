// Involver/Models/ViewModels/TagCloudItemViewModel.cs
namespace Involver.Models.ViewModels
{
    public class TagCloudItemViewModel
    {
        public string TagName { get; set; }
        public int Count { get; set; }
        public int Size { get; set; } // 用於決定 CSS class
    }
}
