namespace StandETT;

public static class StringExtensions
{
    public static string TrimStart(this string input, string trimString)
    {
        while (input.StartsWith(trimString))
        {
            input = input.Substring(trimString.Length);
        }
        return input;
    }

    public static string TrimEnd(this string input, string trimString)
    {
        while (input.EndsWith(trimString))
        {
            input = input.Substring(0, input.Length - trimString.Length);
        }
        return input;
    }
}