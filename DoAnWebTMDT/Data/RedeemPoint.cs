using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class RedeemPoint
{
    public int RedeemId { get; set; }

    public int AccountId { get; set; }

    public int ProductId { get; set; }

    public int PointsUsed { get; set; }

    public DateTime? RedeemDate { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
