using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Functions
{
    public class MarkdownToHtmlModel : PageModel
    {
        [BindProperty]
        public string MarkdownContent { get; set; }

        public string HtmlContent { get; set; }

        public void OnGet()
        {
            // Initialize with some default content including a table
            MarkdownContent = "# Hello, Markdown!\n\nThis is a sample text.\n\n| Header 1 | Header 2 |\n| -------- | -------- |\n| Cell 1   | Cell 2   |\n| Cell 3   | Cell 4   |\n\n- Item 1\n- Item 2";

            var pipeline = new MarkdownPipelineBuilder()
                .UsePipeTables() // For pipe-style tables
                .UseGridTables() // For grid-style tables
                .Build();

            HtmlContent = Markdown.ToHtml(MarkdownContent ?? string.Empty, pipeline)
                .Replace("<table>", "<table class=\"table table-bordered table-sm\">");
        }

        public void OnPost()
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UsePipeTables() // For pipe-style tables
                .UseGridTables() // For grid-style tables
                .Build();

            HtmlContent = Markdown.ToHtml(MarkdownContent ?? string.Empty, pipeline)
                .Replace("<table>", "<table class=\"table table-bordered table-sm\">");

        }
    }
}

