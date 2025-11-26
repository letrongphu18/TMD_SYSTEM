using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TMD.Models;

public partial class TmdContext : DbContext
{
    public TmdContext()
    {
    }

    public TmdContext(DbContextOptions<TmdContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<LateRequest> LateRequests { get; set; }

    public virtual DbSet<LeaveRequest> LeaveRequests { get; set; }

    public virtual DbSet<LoginHistory> LoginHistories { get; set; }

    public virtual DbSet<OvertimeRequest> OvertimeRequests { get; set; }

    public virtual DbSet<PasswordResetHistory> PasswordResetHistories { get; set; }

    public virtual DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<PasswordResetToken1> PasswordResetTokens1 { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalaryAdjustment> SalaryAdjustments { get; set; }

    public virtual DbSet<SalaryConfigCategory> SalaryConfigCategories { get; set; }

    public virtual DbSet<SalaryConfigHistory> SalaryConfigHistories { get; set; }

    public virtual DbSet<SalaryConfiguration> SalaryConfigurations { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<SystemSettingsBackup20251123> SystemSettingsBackup20251123s { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSalarySetting> UserSalarySettings { get; set; }

    public virtual DbSet<UserTask> UserTasks { get; set; }

    public virtual DbSet<VwActiveSalarySetting> VwActiveSalarySettings { get; set; }

    public virtual DbSet<VwPendingRequestsSummary> VwPendingRequestsSummaries { get; set; }

    public virtual DbSet<VwSalaryCalculation> VwSalaryCalculations { get; set; }

    public virtual DbSet<VwTaskPerformance> VwTaskPerformances { get; set; }

    public virtual DbSet<VwTasksWithDeadline> VwTasksWithDeadlines { get; set; }

    public virtual DbSet<VwTodayAttendance> VwTodayAttendances { get; set; }

    public virtual DbSet<VwUserDetail> VwUserDetails { get; set; }

    public virtual DbSet<WorkScheduleException> WorkScheduleExceptions { get; set; }

   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlServer(
        "Server=tmd-server-system.database.windows.net;Database=TMD;User Id=tmd_admin;Password=MyP@ssword123!;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;");
}


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261CEBF9F49E");

            entity.HasIndex(e => new { e.UserId, e.WorkDate }, "IX_Attendances_UserId_WorkDate");

            entity.Property(e => e.ActualWorkHours).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.ApprovedOvertimeHours).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CheckInAddress).HasMaxLength(500);
            entity.Property(e => e.CheckInIpaddress)
                .HasMaxLength(50)
                .HasColumnName("CheckInIPAddress");
            entity.Property(e => e.CheckInLatitude).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.CheckInLongitude).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.CheckInNotes).HasMaxLength(1000);
            entity.Property(e => e.CheckOutAddress).HasMaxLength(500);
            entity.Property(e => e.CheckOutIpaddress)
                .HasMaxLength(50)
                .HasColumnName("CheckOutIPAddress");
            entity.Property(e => e.CheckOutLatitude).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.CheckOutLongitude).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.CheckOutNotes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DeductionAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DeductionHours).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsLate).HasDefaultValue(false);
            entity.Property(e => e.IsWithinGeofence).HasDefaultValue(true);
            entity.Property(e => e.SalaryMultiplier)
                .HasDefaultValue(1.0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.StandardWorkHours)
                .HasDefaultValue(8m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TotalHours).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.LateRequest).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.LateRequestId)
                .HasConstraintName("FK_Attendances_LateRequest");

            entity.HasOne(d => d.OvertimeRequest).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.OvertimeRequestId)
                .HasConstraintName("FK_Attendances_OvertimeRequest");

            entity.HasOne(d => d.ScheduleException).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.ScheduleExceptionId)
                .HasConstraintName("FK_Attendances_ScheduleException");

            entity.HasOne(d => d.User).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Attendanc__UserI__534D60F1");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK__AuditLog__EB5F6CBD62F10178");

            entity.HasIndex(e => new { e.UserId, e.Timestamp }, "IX_AuditLogs_UserId_Timestamp");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .HasColumnName("IPAddress");
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__AuditLogs__UserI__571DF1D5");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BED40AFE942");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<LateRequest>(entity =>
        {
            entity.HasKey(e => e.LateRequestId).HasName("PK__LateRequ__6814EE075B2D35BA");

            entity.HasIndex(e => e.Status, "IX_LateRequests_Status");

            entity.HasIndex(e => new { e.UserId, e.RequestDate }, "IX_LateRequests_UserId_RequestDate");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ProofDocument).HasMaxLength(500);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.ReviewNote).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.LateRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_LateRequests_Reviewer");

            entity.HasOne(d => d.User).WithMany(p => p.LateRequestUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_LateRequests_User");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.LeaveRequestId).HasName("PK__LeaveReq__609421EE09998BE8");

            entity.HasIndex(e => e.StartDate, "IX_LeaveRequests_StartDate");

            entity.HasIndex(e => new { e.UserId, e.Status }, "IX_LeaveRequests_UserId_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LeaveType).HasMaxLength(50);
            entity.Property(e => e.ProofDocument).HasMaxLength(500);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.ReviewNote).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalDays).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.LeaveRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_LeaveRequests_Reviewer");

            entity.HasOne(d => d.User).WithMany(p => p.LeaveRequestUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_LeaveRequests_User");
        });

        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.HasKey(e => e.LoginHistoryId).HasName("PK__LoginHis__2773EA9F24699591");

            entity.ToTable("LoginHistory");

            entity.HasIndex(e => e.UserId, "IX_LoginHistory_UserId");

            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Device).HasMaxLength(100);
            entity.Property(e => e.FailReason).HasMaxLength(200);
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .HasColumnName("IPAddress");
            entity.Property(e => e.IsSuccess).HasDefaultValue(true);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.LoginHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__LoginHist__UserI__5BE2A6F2");
        });

        modelBuilder.Entity<OvertimeRequest>(entity =>
        {
            entity.HasKey(e => e.OvertimeRequestId).HasName("PK__Overtime__F97D0DCA560E4C6E");

            entity.HasIndex(e => e.ExpiryDate, "IX_OvertimeRequests_ExpiryDate");

            entity.HasIndex(e => new { e.IsExpired, e.Status }, "IX_OvertimeRequests_IsExpired_Status");

            entity.HasIndex(e => new { e.UserId, e.WorkDate }, "IX_OvertimeRequests_UserId_WorkDate");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsExpired).HasDefaultValue(false);
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.ReviewNote).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TaskDescription).HasMaxLength(1000);

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.OvertimeRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_OvertimeRequests_Reviewer");

            entity.HasOne(d => d.User).WithMany(p => p.OvertimeRequestUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_OvertimeRequests_User");
        });

        modelBuilder.Entity<PasswordResetHistory>(entity =>
        {
            entity.HasKey(e => e.ResetId).HasName("PK__Password__783CF04DF61E0EB7");

            entity.ToTable("PasswordResetHistory");

            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .HasColumnName("IPAddress");
            entity.Property(e => e.OldPasswordHash).HasMaxLength(255);
            entity.Property(e => e.ResetReason).HasMaxLength(500);
            entity.Property(e => e.ResetTime).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ResetByUser).WithMany(p => p.PasswordResetHistoryResetByUsers)
                .HasForeignKey(d => d.ResetByUserId)
                .HasConstraintName("FK__PasswordR__Reset__60A75C0F");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetHistoryUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PasswordR__UserI__5FB337D6");
        });

        modelBuilder.Entity<PasswordResetOtp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC07F3B72DCD");

            entity.ToTable("PasswordResetOTPs");

            entity.HasIndex(e => e.Email, "IX_PasswordResetOTPs_Email");

            entity.HasIndex(e => e.OtpCode, "IX_PasswordResetOTPs_OtpCode");

            entity.HasIndex(e => e.UserId, "IX_PasswordResetOTPs_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.ExpiryTime).HasColumnType("datetime");
            entity.Property(e => e.OtpCode).HasMaxLength(6);
            entity.Property(e => e.UsedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetOtps)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_PasswordResetOTPs_Users");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC071B5D66E3");

            entity.ToTable("PasswordResetToken");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.LockoutUntil).HasColumnType("datetime");
            entity.Property(e => e.ResendAvailableAt).HasColumnType("datetime");
            entity.Property(e => e.TokenCode).HasMaxLength(6);

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordResetToken_User");
        });

        modelBuilder.Entity<PasswordResetToken1>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC071EAAF641");

            entity.ToTable("PasswordResetTokens");

            entity.HasIndex(e => e.Token, "IX_PasswordResetTokens_Token");

            entity.HasIndex(e => e.UserId, "IX_PasswordResetTokens_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiryTime).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(500);
            entity.Property(e => e.UsedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetToken1s)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordResetTokens_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A31ADBDE1");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160E7BCAB19").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SalaryAdjustment>(entity =>
        {
            entity.HasKey(e => e.AdjustmentId).HasName("PK__SalaryAd__E60DB893E5A43735");

            entity.HasIndex(e => e.AdjustmentType, "IX_SalaryAdjustments_AdjustmentType");

            entity.HasIndex(e => new { e.UserId, e.WorkDate }, "IX_SalaryAdjustments_UserId_WorkDate");

            entity.Property(e => e.AdjustedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AdjustmentType).HasMaxLength(50);
            entity.Property(e => e.Amount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Hours)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.Reason).HasMaxLength(1000);

            entity.HasOne(d => d.AdjustedByNavigation).WithMany(p => p.SalaryAdjustmentAdjustedByNavigations)
                .HasForeignKey(d => d.AdjustedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SalaryAdj__Adjus__42E1EEFE");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.SalaryAdjustmentApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK__SalaryAdj__Appro__43D61337");

            entity.HasOne(d => d.Attendance).WithMany(p => p.SalaryAdjustments)
                .HasForeignKey(d => d.AttendanceId)
                .HasConstraintName("FK__SalaryAdj__Atten__41EDCAC5");

            entity.HasOne(d => d.User).WithMany(p => p.SalaryAdjustmentUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SalaryAdj__UserI__40F9A68C");
        });

        modelBuilder.Entity<SalaryConfigCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__SalaryCo__19093A0BE0B88366");

            entity.HasIndex(e => e.CategoryCode, "UQ__SalaryCo__371BA955E866D9B3").IsUnique();

            entity.Property(e => e.CategoryCode).HasMaxLength(50);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<SalaryConfigHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__SalaryCo__4D7B4ABDBEE5882D");

            entity.ToTable("SalaryConfigHistory");

            entity.HasIndex(e => e.ChangedBy, "IX_SalaryConfigHistory_ChangedBy");

            entity.HasIndex(e => e.ConfigId, "IX_SalaryConfigHistory_ConfigId");

            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NewValue).HasMaxLength(1000);
            entity.Property(e => e.OldValue).HasMaxLength(1000);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.SalaryConfigHistories)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SalaryCon__Chang__6BE40491");

            entity.HasOne(d => d.Config).WithMany(p => p.SalaryConfigHistories)
                .HasForeignKey(d => d.ConfigId)
                .HasConstraintName("FK__SalaryCon__Confi__6AEFE058");
        });

        modelBuilder.Entity<SalaryConfiguration>(entity =>
        {
            entity.HasKey(e => e.ConfigId).HasName("PK__SalaryCo__C3BC335CE83B99FF");

            entity.HasIndex(e => e.CategoryId, "IX_SalaryConfigurations_CategoryId");

            entity.HasIndex(e => e.IsActive, "IX_SalaryConfigurations_IsActive");

            entity.HasIndex(e => e.ConfigCode, "UQ__SalaryCo__5488CE6808AA130F").IsUnique();

            entity.Property(e => e.ConfigCode).HasMaxLength(100);
            entity.Property(e => e.ConfigName).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DefaultValue).HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayLabel).HasMaxLength(200);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEditable).HasDefaultValue(true);
            entity.Property(e => e.MaxValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.Value).HasMaxLength(1000);
            entity.Property(e => e.ValueType).HasMaxLength(50);

            entity.HasOne(d => d.Category).WithMany(p => p.SalaryConfigurations)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SalaryCon__Categ__681373AD");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.SalaryConfigurationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__SalaryCon__Creat__690797E6");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.SalaryConfigurationUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK__SalaryCon__Updat__69FBBC1F");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PK__SystemSe__54372B1DEA688F4D");

            entity.HasIndex(e => e.Category, "IX_SystemSettings_Category");

            entity.HasIndex(e => new { e.Category, e.ApplyMethod }, "IX_SystemSettings_Category_ApplyMethod").HasFilter("([IsActive]=(1))");

            entity.HasIndex(e => e.IsActive, "IX_SystemSettings_IsActive");

            entity.HasIndex(e => e.IsEnabled, "IX_SystemSettings_IsEnabled");

            entity.HasIndex(e => e.Priority, "IX_SystemSettings_Priority")
                .IsDescending()
                .HasFilter("([IsActive]=(1) AND [IsEnabled]=(1))");

            entity.HasIndex(e => e.SettingKey, "UQ__SystemSe__01E719AD7D8ED9CB").IsUnique();

            entity.Property(e => e.ApplyMethod)
                .HasMaxLength(20)
                .HasDefaultValue("Add");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.MaxValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Priority).HasDefaultValue(0);
            entity.Property(e => e.SettingKey).HasMaxLength(100);
            entity.Property(e => e.SettingValue).HasMaxLength(1000);
            entity.Property(e => e.Unit).HasMaxLength(20);

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.SystemSettings)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK__SystemSet__Updat__0A9D95DB");
        });

        modelBuilder.Entity<SystemSettingsBackup20251123>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("SystemSettings_Backup_20251123");

            entity.Property(e => e.ApplyMethod).HasMaxLength(20);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.SettingId).ValueGeneratedOnAdd();
            entity.Property(e => e.SettingKey).HasMaxLength(100);
            entity.Property(e => e.SettingValue).HasMaxLength(1000);
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Tasks__7C6949B188536639");

            entity.HasIndex(e => e.Deadline, "IX_Tasks_Deadline").HasFilter("([Deadline] IS NOT NULL)");

            entity.HasIndex(e => new { e.IsActive, e.Deadline }, "IX_Tasks_IsActive_Deadline").HasFilter("([Deadline] IS NOT NULL)");

            entity.HasIndex(e => e.Priority, "IX_Tasks_Priority");

            entity.HasIndex(e => e.UpdatedAt, "IX_Tasks_UpdatedAt");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Platform).HasMaxLength(200);
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasDefaultValue("Medium");
            entity.Property(e => e.TargetPerWeek).HasDefaultValue(0);
            entity.Property(e => e.TaskName).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CB231ECDE");

            entity.HasIndex(e => e.DepartmentId, "IX_Users_DepartmentId");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Username, "IX_Users_Username");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4FED6441B").IsUnique();

            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Department).WithMany(p => p.Users)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__Users__Departmen__4222D4EF");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__4316F928");
        });

        modelBuilder.Entity<UserSalarySetting>(entity =>
        {
            entity.HasKey(e => e.UserSalaryId).HasName("PK__UserSala__528D2F424FA45E81");

            entity.HasIndex(e => e.UserId, "UQ__UserSala__1788CC4D6A1C46E6").IsUnique();

            entity.Property(e => e.AllowanceAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BaseSalary)
                .HasDefaultValue(5000000m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DefaultOvertimeRate)
                .HasDefaultValue(1.5m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SalaryType)
                .HasMaxLength(20)
                .HasDefaultValue("Monthly");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.UserSalarySettingCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__UserSalar__Creat__30C33EC3");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.UserSalarySettingUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK__UserSalar__Updat__31B762FC");

            entity.HasOne(d => d.User).WithOne(p => p.UserSalarySettingUser)
                .HasForeignKey<UserSalarySetting>(d => d.UserId)
                .HasConstraintName("FK__UserSalar__UserI__2FCF1A8A");
        });

        modelBuilder.Entity<UserTask>(entity =>
        {
            entity.HasKey(e => e.UserTaskId).HasName("PK__UserTask__4EF5961FD9ECDA05");

            entity.HasIndex(e => e.Status, "IX_UserTasks_Status");

            entity.Property(e => e.CompletedThisWeek).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ReportLink).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("TODO");

            entity.HasOne(d => d.Task).WithMany(p => p.UserTasks)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("FK__UserTasks__TaskI__4D94879B");

            entity.HasOne(d => d.User).WithMany(p => p.UserTasks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserTasks__UserI__4CA06362");
        });

        modelBuilder.Entity<VwActiveSalarySetting>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ActiveSalarySettings");

            entity.Property(e => e.ApplyMethod).HasMaxLength(20);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.MaxValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SettingId).ValueGeneratedOnAdd();
            entity.Property(e => e.SettingKey).HasMaxLength(100);
            entity.Property(e => e.SettingValue).HasMaxLength(1000);
            entity.Property(e => e.Unit).HasMaxLength(20);
        });

        modelBuilder.Entity<VwPendingRequestsSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PendingRequestsSummary");

            entity.Property(e => e.RequestType)
                .HasMaxLength(15)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwSalaryCalculation>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SalaryCalculation");

            entity.Property(e => e.AutoDeduction).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DailySalaryTotal).HasColumnType("numeric(38, 8)");
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.MonthlyAllowance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OvertimeSalary).HasColumnType("numeric(38, 12)");
            entity.Property(e => e.SalaryType).HasMaxLength(20);
            entity.Property(e => e.TotalOvertimeHours).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalWorkHours).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwTaskPerformance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TaskPerformance");

            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Platform).HasMaxLength(200);
            entity.Property(e => e.ReportLink).HasMaxLength(500);
            entity.Property(e => e.TaskName).HasMaxLength(200);
        });

        modelBuilder.Entity<VwTasksWithDeadline>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TasksWithDeadline");

            entity.Property(e => e.DeadlineStatus)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Platform).HasMaxLength(200);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.TaskId).ValueGeneratedOnAdd();
            entity.Property(e => e.TaskName).HasMaxLength(200);
        });

        modelBuilder.Entity<VwTodayAttendance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TodayAttendance");

            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.CheckInAddress).HasMaxLength(500);
            entity.Property(e => e.CheckOutAddress).HasMaxLength(500);
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.TotalHours).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<VwUserDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_UserDetails");

            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<WorkScheduleException>(entity =>
        {
            entity.HasKey(e => e.ExceptionId).HasName("PK__WorkSche__26981D8847CF3126");

            entity.HasIndex(e => e.UserId, "IX_WorkScheduleExceptions_UserId");

            entity.HasIndex(e => e.WorkDate, "IX_WorkScheduleExceptions_WorkDate");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ExceptionType)
                .HasMaxLength(50)
                .HasDefaultValue("Normal");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OvertimeMultiplier).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.SalaryMultiplier)
                .HasDefaultValue(1.0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.StandardHours).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.WorkScheduleExceptionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__WorkSched__Creat__3A4CA8FD");

            entity.HasOne(d => d.Department).WithMany(p => p.WorkScheduleExceptions)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__WorkSched__Depar__395884C4");

            entity.HasOne(d => d.User).WithMany(p => p.WorkScheduleExceptionUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__WorkSched__UserI__3864608B");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
