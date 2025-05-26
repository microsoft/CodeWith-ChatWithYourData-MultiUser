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
    }
}
