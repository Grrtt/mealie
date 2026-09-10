using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Mealie.Api.Controllers.Recipes;

/// <summary>
///     Sets the Content-Type and Content-Disposition headers for the streamed single-recipe ZIP
///     download, matching the headers an MVC <see cref="Microsoft.AspNetCore.Mvc.FileResult" />
///     produces (including <c>filename*</c> encoding for non-ASCII file names).
/// </summary>
public static class RecipeExportResponseHeaders
{
    public static void Apply(HttpResponse response, string fileName)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(fileName);

        response.ContentType = "application/zip";

        var contentDisposition = new ContentDispositionHeaderValue("attachment");
        contentDisposition.SetHttpFileName(fileName);
        response.Headers.ContentDisposition = contentDisposition.ToString();
    }
}
