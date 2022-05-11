namespace Involver.Helpers
{
    public static class PageSizeHelper
    {
        /// <summary>
        /// 取得第一頁與最後一頁，顯示的總頁數為五，PageIndex時常置中
        /// </summary>
        /// <param name="pageIndex">指定頁數</param>
        /// <param name="totalPages">總頁數</param>
        /// <param name="startPage">第一頁</param>
        /// <param name="endPage">最後一頁</param>
        public static void Get(int pageIndex, int totalPages, ref int startPage, ref int endPage)
        {
            if (pageIndex > 2)
            {
                startPage = pageIndex - 2;
            }
            else if (pageIndex < 3)
            {
                startPage = 1;
            }
            else
            {
                startPage = pageIndex;
            }

            endPage = startPage + 4;
            if (endPage > totalPages)
            {
                endPage = totalPages;
            }
        }
    }
}
