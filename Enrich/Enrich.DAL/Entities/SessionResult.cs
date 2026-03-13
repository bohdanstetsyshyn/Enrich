using System;
using System.Collections.Generic;

namespace Enrich.Infrastructure.Models;

public partial class SessionResult
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public int WordId { get; set; }

    public bool IsCorrect { get; set; }

    public virtual TrainingSession Session { get; set; } = null!;

    public virtual Word Word { get; set; } = null!;
}
