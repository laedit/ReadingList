using System;

namespace AddBook.Business.Generation
{
    internal static class DateIndexGenerator
    {
        internal const string Content = """
+++
sort_by = "date"
transparent = true
render = false
+++
""";

        internal static string GetYearIndexPath(DateTime postDate)
        {
            return $"{SiteHelper.PostsFolder}/{postDate.Year}/_index.md";
        }

        internal static string GetMonthIndexPath(DateTime postDate)
        {
            return $"{SiteHelper.PostsFolder}/{postDate.Year}/{postDate:MM}/_index.md";
        }

        internal static string GetDayIndexPath(DateTime postDate)
        {
            return $"{SiteHelper.PostsFolder}/{postDate.Year}/{postDate:MM}/{postDate:dd}/_index.md";
        }
    }
}
