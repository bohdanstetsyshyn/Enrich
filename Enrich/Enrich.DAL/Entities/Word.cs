using System;
using System.Collections.Generic;

namespace Enrich.Infrastructure.Models;

public partial class Word
{
    public int Id { get; set; }

    public string Term { get; set; } = null!;

    public string? Definition { get; set; }

    public string? Translation { get; set; }

    public string? DifficultyLevel { get; set; }

    public bool IsGlobal { get; set; }

    public int? CreatorId { get; set; }

    public virtual ICollection<CommunityImport> CommunityImports { get; set; } = new List<CommunityImport>();

    public virtual User? Creator { get; set; }

    public virtual ICollection<SessionResult> SessionResults { get; set; } = new List<SessionResult>();

    public virtual ICollection<WordProgress> WordProgresses { get; set; } = new List<WordProgress>();

    public virtual ICollection<Bundle> Bundles { get; set; } = new List<Bundle>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
