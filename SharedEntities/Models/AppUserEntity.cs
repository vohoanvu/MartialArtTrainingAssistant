using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models;

/// <summary>
/// The model for the application user.
/// </summary>
public class AppUserEntity : IdentityUser
{
    /// <summary>
    /// Date and time the user was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Date and time the user was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Corresponding Fighter data for the user
    /// </summary>
    public int FighterId { get; set; }
    [ForeignKey("FighterId")]
    public virtual Fighter? Fighter { get; set; }
}