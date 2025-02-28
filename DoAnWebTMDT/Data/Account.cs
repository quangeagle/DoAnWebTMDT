using System;
using System.Collections.Generic;

namespace DoAnWebTMDT.Data;

public partial class Account
{
    public int AccountId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? FullName { get; set; }

    public string? Address { get; set; }

    public string Role { get; set; } = null!;

    public int? RewardPoints { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string OTP { get; set; }

    public DateTime? OTP_Expiry { get; set; }


    public virtual ICollection<Chat> ChatReceivers { get; set; } = new List<Chat>();

    public virtual ICollection<Chat> ChatSenders { get; set; } = new List<Chat>();

    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    public virtual ICollection<LikeList> LikeLists { get; set; } = new List<LikeList>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<RedeemPoint> RedeemPoints { get; set; } = new List<RedeemPoint>();
}
