using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApi.Helpers
{
    public static class Utilities
    {
        internal static string SanitizeFileName(string fileName)
        {
            var sanitizedFileName = new StringBuilder();
            foreach (var c in fileName)
            {
                if (c <= sbyte.MaxValue)
                {
                    sanitizedFileName.Append(c);
                }
                else
                {
                    sanitizedFileName.Append('_'); // Replace non-ASCII characters with an underscore
                }
            }
            return sanitizedFileName.ToString();
        }

       

        internal static int ExtractRetryAfterSeconds(string message)
        {
            // Define a regular expression to match the retry duration in seconds
            var regex = new Regex(@"Try again in (\d+) seconds", RegexOptions.IgnoreCase);
            var match = regex.Match(message);

            if (match.Success && int.TryParse(match.Groups[1].Value, out int retryAfterSeconds))
            {
                return retryAfterSeconds;
            }

            // Return a default value if the retry duration is not found
            return 60; // Default to 60 seconds
        }
    }


}
