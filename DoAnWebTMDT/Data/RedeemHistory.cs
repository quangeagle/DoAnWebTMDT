using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class RedeemHistory
{
    public int RedeemId { get; set; }

    public int AccountId { get; set; }

    public int PointsUsed { get; set; }

    public decimal DiscountAmount { get; set; }

    public DateTime? RedeemDate { get; set; }

    public virtual Account Account { get; set; } = null!;
}
