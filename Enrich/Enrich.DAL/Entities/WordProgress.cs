using System;
using System.Collections.Generic;

namespace Enrich.Infrastructure.Models;

public partial class WordProgress
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int WordId { get; set; }

    public string? Status { get; set; }

    public double? SuccessRate { get; set; }

    public DateTime? LastReviewed { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Word Word { get; set; } = null!;
}
