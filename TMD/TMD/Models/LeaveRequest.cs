using System;
using System.Collections.Generic;

namespace TMD.Models;

public partial class LeaveRequest
{
    public int LeaveRequestId { get; set; }

    public int UserId { get; set; }

    public string LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TotalDays { get; set; }

    public string Reason { get; set; } = null!;

    public string? ProofDocument { get; set; }

    public string Status { get; set; } = null!;

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
