using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Attributes;

/// <summary>
/// A filter that specifies the type of the value and status code returned by the action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class JsonResponseAttribute : Attribute, IApiResponseMetadataProvider
{
    private readonly MediaTypeCollection? _contentTypes;
        
        
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonResponseAttribute"/> class with default settings.
    /// </summary>
    public JsonResponseAttribute()
    {
        IsResponseTypeSetByDefault = true;
        _contentTypes = new MediaTypeCollection
        {
            "application/json"
        };
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonResponseAttribute"/> class with the specified type.
    /// </summary>
    /// <param name="type">The type of the value returned by an action.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public JsonResponseAttribute(Type type)
    {
        _contentTypes = ["application/json"];
        Type = type ?? throw new ArgumentNullException(nameof(type));
        StatusCode = 200;
        IsResponseTypeSetByDefault = false;
    }
        
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonResponseAttribute"/> class with the specified status code and type.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="type">The type of the value returned by an action.</param>
    public JsonResponseAttribute(int statusCode, Type type)
    {
        _contentTypes = ["application/json"];
        Type = type ?? throw new ArgumentNullException(nameof(type));
        StatusCode = statusCode;
        IsResponseTypeSetByDefault = false;
    }
        
        
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonResponseAttribute"/> class with the specified type, status code, content type, and additional content types.
    /// </summary>
    /// <param name="type">The type of the value returned by an action.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="contentType">The primary content type of the response.</param>
    /// <param name="additionalContentTypes">Additional content types for the response.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contentType"/> is null.</exception>
    public JsonResponseAttribute(Type type, int statusCode, string contentType, params string[] additionalContentTypes)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        Type = type ?? throw new ArgumentNullException(nameof(type));
        StatusCode = statusCode;
        IsResponseTypeSetByDefault = false;

        MediaTypeHeaderValue.Parse(contentType);
        foreach (var t in additionalContentTypes)
            MediaTypeHeaderValue.Parse(t);

        _contentTypes = GetContentTypes(contentType, additionalContentTypes);
    }

    /// <summary>
    /// Gets or sets the type of the value returned by an action.
    /// </summary>
    public Type Type { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }

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
    internal bool IsResponseTypeSetByDefault { get; }

    /// <inheritdoc />
    void IApiResponseMetadataProvider.SetContentTypes(MediaTypeCollection contentTypes)
    {
        if (_contentTypes is not null)
        {
            contentTypes.Clear();
            foreach (var contentType in _contentTypes)
            {
                contentTypes.Add(contentType);
            }
        }
    }

    /// <summary>
    /// Gets the content types from the specified content type and additional content types.
    /// </summary>
    /// <param name="contentType">The primary content type.</param>
    /// <param name="additionalContentTypes">Additional content types.</param>
    /// <returns>A <see cref="MediaTypeCollection"/> containing the content types.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a wildcard content type is specified.</exception>
    private static MediaTypeCollection GetContentTypes(string contentType, string[] additionalContentTypes)
    {
        var completeContentTypes = new List<string>(additionalContentTypes.Length + 1) { contentType };
        completeContentTypes.AddRange(additionalContentTypes);
        MediaTypeCollection contentTypes = new();
        foreach (var type in completeContentTypes)
        {
            var mediaType = new MediaType(type);
            if (mediaType.HasWildcard)
            {
                throw new InvalidOperationException("Wildcards are not supported.");
            }

            contentTypes.Add(type);
        }

        return contentTypes;
    }
}