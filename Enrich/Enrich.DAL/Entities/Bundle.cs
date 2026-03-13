using System;
using System.Collections.Generic;

namespace Enrich.Infrastructure.Models;

public partial class Bundle
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int OwnerId { get; set; }

    public string? Status { get; set; }

    public bool IsSystem { get; set; }

    public int? AdminId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? Admin { get; set; }

    public virtual ICollection<CommunityImport> CommunityImports { get; set; } = new List<CommunityImport>();

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<TrainingSession> TrainingSessions { get; set; } = new List<TrainingSession>();

    public virtual ICollection<Word> Words { get; set; } = new List<Word>();
}
