using System.ComponentModel.DataAnnotations;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Account;

/// <summary>
/// Represents the request body for updating billing information.
/// </summary>
public class UpdateBillingInfoRequestBody
{
    /// <summary>
    /// Gets or sets the first name of the user.
    /// </summary>
    [Required]
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    [Required]
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets the primary address line.
    /// </summary>
    [Required]
    public required string AddressOne { get; set; }

    /// <summary>
    /// Gets or sets the secondary address line (optional).
    /// </summary>
    public string? AddressTwo { get; set; }

    /// <summary>
    /// Gets or sets the city of the user (optional).
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the postal code of the user.
    /// </summary>
    [Required]
    public required string PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the country of the user.
    /// </summary>
    [Required]
    public required string Country { get; set; }
}