public class DateJustifyFilter : IFilter
{
    public string Name
    {
        get { return "DateJustify"; }
    }

    public static string DateJustify(DateTime input)
    {
        var day = input.ToString("dd");
        var year = input.ToString("yy");
        var month = input.ToString("MMM").PadRight(5, ' '); // unbreakable space
        return day + " " + month + " " + year;
    }
}