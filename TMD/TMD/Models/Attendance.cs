using System;
using System.Collections.Generic;

namespace TMD.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int UserId { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public decimal? CheckInLatitude { get; set; }

    public decimal? CheckInLongitude { get; set; }

    public string? CheckInAddress { get; set; }

    public string? CheckInPhotos { get; set; }

    public string? CheckInFiles { get; set; }

    public string? CheckInNotes { get; set; }

    public string? CheckInIpaddress { get; set; }

    public bool? IsWithinGeofence { get; set; }

    public bool? IsLate { get; set; }

    public decimal? CheckOutLatitude { get; set; }

    public decimal? CheckOutLongitude { get; set; }

    public string? CheckOutAddress { get; set; }

    public string? CheckOutPhotos { get; set; }

    public string? CheckOutFiles { get; set; }

    public string? CheckOutNotes { get; set; }

    public string? CheckOutIpaddress { get; set; }

    public decimal? TotalHours { get; set; }

    public DateOnly WorkDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool HasLateRequest { get; set; }

    public int? LateRequestId { get; set; }

    public bool HasOvertimeRequest { get; set; }

    public int? OvertimeRequestId { get; set; }

    public bool IsOvertimeApproved { get; set; }

    public decimal ApprovedOvertimeHours { get; set; }

    public decimal DeductionHours { get; set; }

    public decimal DeductionAmount { get; set; }

    public virtual LateRequest? LateRequest { get; set; }

    public virtual OvertimeRequest? OvertimeRequest { get; set; }

    public virtual User User { get; set; } = null!;
}
