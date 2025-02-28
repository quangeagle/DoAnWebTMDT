using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal OldPrice { get; set; }

    public decimal? NewPrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public int Stock { get; set; }

    public string? ImageUrl { get; set; }

    public int? CategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? MediaPath { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    public virtual ICollection<GuestCart> GuestCarts { get; set; } = new List<GuestCart>();

    public virtual ICollection<GuestLikeList> GuestLikeLists { get; set; } = new List<GuestLikeList>();

    public virtual ICollection<LikeList> LikeLists { get; set; } = new List<LikeList>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<RedeemPoint> RedeemPoints { get; set; } = new List<RedeemPoint>();
}
