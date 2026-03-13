using System;
using System.Collections.Generic;

namespace Enrich.Infrastructure.Models;

public partial class TrainingSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? BundleId { get; set; }

    public int? Score { get; set; }

    public string? TrainingType { get; set; }

    public DateTime StartTime { get; set; }

    public virtual Bundle? Bundle { get; set; }

    public virtual ICollection<SessionResult> SessionResults { get; set; } = new List<SessionResult>();

    public virtual User User { get; set; } = null!;
}
