using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Tavstal.YggdrasilSharp.Models.Attributes;

/// <summary>
/// Specifies that the target class or method produces a text-based response.
/// </summary>
/// <remarks>
/// This attribute can be applied to classes or methods to define the response type and content type.
/// It implements <see cref="IApiResponseTypeMetadataProvider"/> to provide metadata about the API response.
/// </remarks>
/// <example>
/// [TextResponseAttribute(200)]
/// public IActionResult GetTextResponse() => Content("Hello, World!");
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class TextResponseAttribute : Attribute, IApiResponseTypeMetadataProvider
{
    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    public TextResponseAttribute(int statusCode)
    {
        IsResponseTypeSetByDefault = false;
        Type = typeof(string);

        string contentType = "text/plain";
        StatusCode = statusCode;
        MediaTypeHeaderValue.Parse(contentType);
        ContentTypes = GetContentTypes(contentType, []);
    }

    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    /// <param name="type"></param>
    public TextResponseAttribute(int statusCode, Type type)
    {
        IsResponseTypeSetByDefault = false;
        Type = type;

        string contentType = "application/json";
        StatusCode = statusCode;
        MediaTypeHeaderValue.Parse(contentType);
        ContentTypes = GetContentTypes(contentType, []);
    }

    /// <summary>
    /// Gets or sets the type of the value returned by an action.
    /// </summary>
    public Type Type;

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode;

    /// <summary>
    /// Used to distinguish a `Type` set by default in the constructor versus
    /// one provided by the user.
    ///
    /// When <see langword="false"/>, then <see cref="Type"/> is set by user.
    ///
    /// When <see langword="true"/>, then <see cref="Type"/> is set by by
    /// default in the constructor
    /// </summary>
    /// <value></value>
    internal bool IsResponseTypeSetByDefault;

    // Internal for testing
    // ReSharper disable once UnusedAutoPropertyAccessor.Global - This property is used by API explorer to determine the content types for this response type, but it is not accessed directly in code.
    internal MediaTypeCollection? ContentTypes { get; }


    /// <summary>
    /// Helper that creates a <see cref="MediaTypeCollection"/> from the provided primary <paramref name="contentType"/>
    /// and any <paramref name="additionalContentTypes"/>.
    /// </summary>
    /// <param name="contentType">Primary content type to include (e.g. "text/plain").</param>
    /// <param name="additionalContentTypes">Optional additional content types to include in the collection.</param>
    /// <returns>A <see cref="MediaTypeCollection"/> containing the merged content types.</returns>
    private static MediaTypeCollection GetContentTypes(string contentType, string[] additionalContentTypes)
    {
        var completeContentTypes = new List<string>(additionalContentTypes.Length + 1) { contentType };
        completeContentTypes.AddRange(additionalContentTypes);
        MediaTypeCollection contentTypes = new();
        foreach (var type in completeContentTypes)
        {
            //var mediaType = new MediaType(type);
            contentTypes.Add(type);
        }

        return contentTypes;
    }

    /// <summary>
    /// Returns the list of supported content types for the given <paramref name="objectType"/>.
    /// </summary>
    /// <param name="contentType">Optional incoming content type (unused by this attribute).</param>
    /// <param name="objectType">The CLR type for which supported content types are requested.</param>
    /// <returns>An <see cref="IReadOnlyList{String}"/> describing supported content types.</returns>
    /// <remarks>
    /// This implementation returns an empty list; the attribute mainly exposes <see cref="ContentTypes"/>
    /// for API explorer consumers. The method is present to satisfy the interface contract.
    /// </remarks>
    public IReadOnlyList<string> GetSupportedContentTypes(string? contentType, Type objectType)
    {
        // Unused, but the attribute requires it
        return new List<string>();
    }
}