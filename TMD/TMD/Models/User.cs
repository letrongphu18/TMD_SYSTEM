using System;
using System.Collections.Generic;

namespace TMD.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Avatar { get; set; }

    public int? DepartmentId { get; set; }

    public int RoleId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<LateRequest> LateRequestReviewedByNavigations { get; set; } = new List<LateRequest>();

    public virtual ICollection<LateRequest> LateRequestUsers { get; set; } = new List<LateRequest>();

    public virtual ICollection<LeaveRequest> LeaveRequestReviewedByNavigations { get; set; } = new List<LeaveRequest>();

    public virtual ICollection<LeaveRequest> LeaveRequestUsers { get; set; } = new List<LeaveRequest>();

    public virtual ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();

    public virtual ICollection<OvertimeRequest> OvertimeRequestReviewedByNavigations { get; set; } = new List<OvertimeRequest>();

    public virtual ICollection<OvertimeRequest> OvertimeRequestUsers { get; set; } = new List<OvertimeRequest>();

    public virtual ICollection<PasswordResetHistory> PasswordResetHistoryResetByUsers { get; set; } = new List<PasswordResetHistory>();

    public virtual ICollection<PasswordResetHistory> PasswordResetHistoryUsers { get; set; } = new List<PasswordResetHistory>();

    public virtual ICollection<PasswordResetOtp> PasswordResetOtps { get; set; } = new List<PasswordResetOtp>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();

    public virtual ICollection<UserTask> UserTasks { get; set; } = new List<UserTask>();
}
