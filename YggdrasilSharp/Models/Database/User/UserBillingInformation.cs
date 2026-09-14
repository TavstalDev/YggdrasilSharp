using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Database.User;

/// <summary>
/// Represents the billing information of a user, including personal details
/// and address information.
/// </summary>
public class UserBillingInformation
{
    /// <summary>
    /// Gets or sets the unique identifier for the user associated with the billing information.
    /// </summary>
    [Key]
    [StringLength(36)]
    public string UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the first name of the user.
    /// </summary>
    [PersonalData]
    [StringLength(50)]
    public string FirstName { get; set; }
    
    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    [PersonalData]
    [StringLength(50)]
    public string LastName { get; set; }
    
    /// <summary>
    /// Gets or sets the primary address of the user.
    /// </summary>
    [PersonalData]
    [StringLength(255)]
    public string AddressOne { get; set; }
    
    /// <summary>
    /// Gets or sets the secondary address of the user, if any.
    /// </summary>
    [PersonalData]
    [StringLength(255)]
    public string? AddressTwo { get; set; }
    
    /// <summary>
    /// Gets or sets the city of the user.
    /// </summary>
    [PersonalData]
    [StringLength(100)]
    public string? City { get; set; }
    
    /// <summary>
    /// Gets or sets the postal code of the user's address.
    /// </summary>
    [PersonalData]
    [StringLength(20)]
    public string PostalCode { get; set; }
    
    /// <summary>
    /// Gets or sets the country of the user.
    /// </summary>
    [PersonalData]
    [StringLength(100)]
    public string Country { get; set; }
    
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */

    /// <summary>
    /// Gets or sets the user associated with the billing information.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [ForeignKey("UserId")]
    [JsonIgnore]
    public CustomUser? User { get; set; }
}
