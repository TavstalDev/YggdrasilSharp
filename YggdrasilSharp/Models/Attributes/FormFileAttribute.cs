using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Tavstal.YggdrasilSharp.Models.Common;

namespace Tavstal.YggdrasilSharp.Models.Attributes;

/// <summary>
/// Attribute for validating uploaded form files.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class FormFileAttribute : ValidationAttribute
{
    private readonly int _maxFileSize;
    private readonly EFileSizeUnit _fileSizeUnit;
    private readonly string[]? _contentTypes;
    private readonly string[]? _fileExtensions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormFileAttribute"/> class.
    /// </summary>
    /// <param name="maxFileSize">The maximum allowed file size.</param>
    /// <param name="fileSizeUnit">The unit of measurement for the file size.</param>
    /// <param name="contentTypes">The allowed content types for the file.</param>
    /// <param name="fileExtensions">The allowed file extensions for the file.</param>
    public FormFileAttribute(int maxFileSize = 10, EFileSizeUnit fileSizeUnit = EFileSizeUnit.Megabytes, string[]? contentTypes = null, string[]? fileExtensions = null)
    {
        _maxFileSize = maxFileSize;
        _fileSizeUnit = fileSizeUnit;
        _contentTypes = contentTypes;
        _fileExtensions = fileExtensions;
    }

    /// <summary>
    /// Validates the uploaded file(s) against the specified constraints.
    /// </summary>
    /// <param name="value">The value to validate, which can be a single file or a collection of files.</param>
    /// <param name="validationContext">The context in which the validation is performed.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the validation was successful.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
            return ValidateFile(file, validationContext);

        if (value is IEnumerable<IFormFile> files)
        {
            foreach (var formFile in files)
            {
                var result = ValidateFile(formFile, validationContext);
                if (result != ValidationResult.Success)
                    return result;
            }
        }

        return ValidationResult.Success;
    }

    private ValidationResult? ValidateFile(IFormFile file, ValidationContext validationContext)
    {
        var fileSize = file.Length;

        if (fileSize == 0)
        {
            // An empty file is treated as "not provided" on optional members,
            // but is rejected on required ones.
            return IsRequiredMember(validationContext)
                ? new ValidationResult("The file is empty.")
                : ValidationResult.Success;
        }

        if (_contentTypes != null && !_contentTypes.Contains(file.ContentType))
            return new ValidationResult($"The media type of the file is not supported. Supported: {string.Join(", ", _contentTypes)}.");

        if (_fileExtensions != null && !_fileExtensions.Any(x => file.FileName.EndsWith(x)))
            return new ValidationResult($"The extension of the file is not supported. Supported: {string.Join(", ", _fileExtensions)}");

        if (fileSize > (long)_maxFileSize * (int)_fileSizeUnit)
            return new ValidationResult($"The file size is greater than {_maxFileSize} {Enum.GetName(_fileSizeUnit)}.");

        return ValidationResult.Success;
    }

    private static bool IsRequiredMember(ValidationContext validationContext)
    {
        var property = validationContext.ObjectType?.GetProperty(validationContext.MemberName ?? string.Empty);
        if (property == null)
            return true;

        if (property.IsDefined(typeof(RequiredAttribute), inherit: true))
            return true;

        var nullability = new NullabilityInfoContext().Create(property);
        return nullability.WriteState != NullabilityState.Nullable;
    }
}
