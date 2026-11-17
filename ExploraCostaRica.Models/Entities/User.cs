using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExploraCostaRica.Models.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    // Hash de la contraseña

    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [MaxLength(20)]
    public string Role { get; set; } = Roles.Usuario;

    public virtual ICollection<Download> Downloads { get; set; } = new List<Download>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
}
