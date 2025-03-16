using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class LoyaltyPoint
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public int TotalPoints { get; set; }

    public virtual Account Account { get; set; } = null!;
}
