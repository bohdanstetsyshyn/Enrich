using System;
using System.Collections.Generic;

namespace Enrich.Infrastructure.Models;

public partial class CommunityImport
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? BundleId { get; set; }

    public int? WordId { get; set; }

    public DateTime ImportedAt { get; set; }

    public virtual Bundle? Bundle { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Word? Word { get; set; }
}
