using System.Net.Mail;
using System.Text.RegularExpressions;

namespace LeadHub.Application;

internal static partial class SlugValidator
{
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Pattern();

    public static bool IsValid(string slug) => Pattern().IsMatch(slug);
}

internal static class UrlValidator
{
    public static bool IsValid(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

internal static class EmailValidator
{
    public static bool IsValid(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
