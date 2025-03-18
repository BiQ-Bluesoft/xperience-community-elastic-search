using System.Text;

namespace Kentico.Xperience.ElasticSearch.Helpers.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// This method processes all characters one by one and removes whitespaces using the <see cref="char.IsWhiteSpace(char)"/> method.
    /// </summary>
    /// <param name="source">Source string.</param>
    /// <returns>Source string without whitespaces.</returns>
    public static string RemoveWhitespacesUsingStringBuilder(this string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            if (!char.IsWhiteSpace(c))
            {
                builder.Append(c);
            }
        }

        return source.Length == builder.Length ? source : builder.ToString();
    }
}
