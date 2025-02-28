using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? MediaPath { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
