using System;
using System.Collections.Generic;

namespace TMD.Models;

public partial class SystemSetting
{
    public int SettingId { get; set; }

    public string SettingKey { get; set; } = null!;

    public string? SettingValue { get; set; }

    public string? Description { get; set; }

    public string? DataType { get; set; }

    public string? Category { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}
