using System;
using System.Collections.Generic;

namespace Enrich.Infrastructure.Models;

public partial class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Word> Words { get; set; } = new List<Word>();
}
