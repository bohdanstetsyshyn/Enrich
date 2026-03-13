using System;
using System.Collections.Generic;

namespace Enrich.Infrastructure.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public string? ThemePreference { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Bundle> BundleAdmins { get; set; } = new List<Bundle>();

    public virtual ICollection<Bundle> BundleOwners { get; set; } = new List<Bundle>();

    public virtual ICollection<CommunityImport> CommunityImports { get; set; } = new List<CommunityImport>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<TrainingSession> TrainingSessions { get; set; } = new List<TrainingSession>();

    public virtual ICollection<WordProgress> WordProgresses { get; set; } = new List<WordProgress>();

    public virtual ICollection<Word> Words { get; set; } = new List<Word>();
}
