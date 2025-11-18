using System;
using System.Collections.Generic;

namespace TMD.Models;

public partial class OvertimeRequest
{
    public int OvertimeRequestId { get; set; }

    public int UserId { get; set; }

    public DateOnly WorkDate { get; set; }

    public DateTime ActualCheckOutTime { get; set; }

    public decimal OvertimeHours { get; set; }

    public string Reason { get; set; } = null!;

    public string? TaskDescription { get; set; }

    public string Status { get; set; } = null!;

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime ExpiryDate { get; set; }

    public bool? IsExpired { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
