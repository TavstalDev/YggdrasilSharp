using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Database.User;

/// <summary>
/// Represents a custom role in the system, extending the IdentityRole class with additional properties
/// such as Level, Name, NormalizedName, and ConcurrencyStamp.
/// </summary>
public class CustomRole : IdentityRole<string>
{
    /// <summary>
    /// Gets or sets the unique identifier for the role.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [StringLength(36)]
    public override string Id { get; set; }
    
    /// <summary>
    /// Gets or sets the level of the role, which can be used to define role hierarchy or permissions.
    /// </summary>
    public byte Level { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the role.
    /// </summary>
    [StringLength(32)]
    public new string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the normalized name of the role, typically used for case-insensitive comparisons.
    /// </summary>
    [StringLength(64)]
    public new string NormalizedName { get; set; }
    
    /// <summary>
    /// Gets or sets the concurrency stamp, a unique value used to handle concurrency conflicts.
    /// </summary>
    [StringLength(36)]
    public new string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRole"/> class with the specified level, name, and normalized name.
    /// </summary>
    /// <param name="level">The level of the role.</param>
    /// <param name="name">The name of the role.</param>
    /// <param name="normalizedName">The normalized name of the role.</param>
    public CustomRole(byte level, string name, string normalizedName)
    {
        Level = level;
        Name = name;
        NormalizedName = normalizedName;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRole"/> class with the specified role name, level, name, and normalized name.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="level">The level of the role.</param>
    /// <param name="name">The name of the role.</param>
    /// <param name="normalizedName">The normalized name of the role.</param>
    public CustomRole(string roleName, byte level, string name, string normalizedName) : base(roleName)
    {
        Level = level;
        Name = name;
        NormalizedName = normalizedName;
    }
    
    /// <summary>
    /// Collection navigation for the user-role join table.
    /// Marked with InverseProperty so EF pairs this with CustomUserRole.Role and avoids creating a shadow FK.
    /// </summary>
    [InverseProperty(nameof(CustomUserRole.Role))]
    [JsonIgnore]
    public virtual ICollection<CustomUserRole> UserRoles { get; set; } = new List<CustomUserRole>();
}
