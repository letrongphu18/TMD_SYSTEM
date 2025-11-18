using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDSystem.Helpers;
using TMD.Models;

namespace TMDSystem.Controllers
{
	public class SettingsController : Controller
	{
		private readonly TmdContext _context;
		private readonly AuditHelper _auditHelper;

		public SettingsController(TmdContext context, AuditHelper auditHelper)
		{
			_context = context;
			_auditHelper = auditHelper;
		}

		private bool IsAdmin()
		{
			return HttpContext.Session.GetString("RoleName") == "Admin";
		}

		// ============================================
		// SETTINGS PAGE
		// ============================================
		[HttpGet]
		public async System.Threading.Tasks.Task<IActionResult> Index()
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			var settings = await _context.SystemSettings
				.Where(s => s.IsActive == true)
				.OrderBy(s => s.Category)
				.ThenBy(s => s.SettingKey)
				.ToListAsync();

			// Nếu chưa có settings, tạo mới với giá trị mặc định
			if (!settings.Any())
			{
				await InitializeDefaultSettings();
				settings = await _context.SystemSettings.ToListAsync();
			}

			await _auditHelper.LogViewAsync(
				HttpContext.Session.GetInt32("UserId").Value,
				"SystemSettings",
				0,
				"Xem trang cấu hình hệ thống"
			);

			return View(settings);
		}

		// ============================================
		// GET SETTING BY KEY
		// ============================================
		[HttpGet]
		public async System.Threading.Tasks.Task<IActionResult> GetSetting(string key)
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Không có quyền truy cập!" });

			var setting = await _context.SystemSettings
				.FirstOrDefaultAsync(s => s.SettingKey == key);

			if (setting == null)
				return Json(new { success = false, message = "Không tìm thấy cấu hình!" });

			return Json(new
			{
				success = true,
				setting = new
				{
					setting.SettingId,
					setting.SettingKey,
					setting.SettingValue,
					setting.Description,
					setting.DataType,
					setting.Category,
					setting.IsActive,
					setting.UpdatedAt
				}
			});
		}

		// ============================================
		// UPDATE SINGLE SETTING
		// ============================================
		[HttpPost]
		public async System.Threading.Tasks.Task<IActionResult> UpdateSetting([FromBody] UpdateSettingRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"SystemSettings",
					"Không có quyền cập nhật",
					new { SettingKey = request.SettingKey }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			var setting = await _context.SystemSettings
				.FirstOrDefaultAsync(s => s.SettingKey == request.SettingKey);

			if (setting == null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"SystemSettings",
					"Setting không tồn tại",
					new { SettingKey = request.SettingKey }
				);

				return Json(new { success = false, message = "Không tìm thấy cấu hình!" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");

				var oldValue = setting.SettingValue;

				setting.SettingValue = request.SettingValue;
				setting.UpdatedAt = DateTime.Now;
				setting.UpdatedBy = adminId;

				await _context.SaveChangesAsync();

				await _auditHelper.LogDetailedAsync(
					adminId,
					"UPDATE",
					"SystemSettings",
					setting.SettingId,
					new { SettingValue = oldValue },
					new { SettingValue = request.SettingValue },
					$"Cập nhật cấu hình: {setting.SettingKey}",
					new Dictionary<string, object>
					{
						{ "OldValue", oldValue ?? "null" },
						{ "NewValue", request.SettingValue ?? "null" },
						{ "UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
					}
				);

				return Json(new
				{
					success = true,
					message = "Cập nhật cấu hình thành công!"
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"SystemSettings",
					$"Exception: {ex.Message}",
					new { SettingKey = request.SettingKey, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		// ============================================
		// BATCH UPDATE SETTINGS (MỚI THÊM)
		// ============================================
		[HttpPost]
		public async System.Threading.Tasks.Task<IActionResult> BatchUpdateSettings([FromBody] List<UpdateSettingRequest> requests)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"SystemSettings",
					"Không có quyền cập nhật hàng loạt",
					new { Count = requests?.Count ?? 0 }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			if (requests == null || !requests.Any())
			{
				return Json(new { success = false, message = "Không có dữ liệu để cập nhật!" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");
				var updatedCount = 0;
				var failedKeys = new List<string>();

				foreach (var request in requests)
				{
					var setting = await _context.SystemSettings
						.FirstOrDefaultAsync(s => s.SettingKey == request.SettingKey);

					if (setting == null)
					{
						failedKeys.Add(request.SettingKey);
						continue;
					}

					var oldValue = setting.SettingValue;

					setting.SettingValue = request.SettingValue;
					setting.UpdatedAt = DateTime.Now;
					setting.UpdatedBy = adminId;

					// Log từng thay đổi
					await _auditHelper.LogDetailedAsync(
						adminId,
						"UPDATE",
						"SystemSettings",
						setting.SettingId,
						new { SettingValue = oldValue },
						new { SettingValue = request.SettingValue },
						$"Batch update: {setting.SettingKey}",
						new Dictionary<string, object>
						{
							{ "OldValue", oldValue ?? "null" },
							{ "NewValue", request.SettingValue ?? "null" }
						}
					);

					updatedCount++;
				}

				await _context.SaveChangesAsync();

				// Log tổng kết
				await _auditHelper.LogDetailedAsync(
					adminId,
					"BATCH_UPDATE",
					"SystemSettings",
					null,
					null,
					null,
					$"Batch update hoàn tất: {updatedCount}/{requests.Count}",
					new Dictionary<string, object>
					{
						{ "TotalRequests", requests.Count },
						{ "UpdatedCount", updatedCount },
						{ "FailedCount", failedKeys.Count },
						{ "FailedKeys", string.Join(", ", failedKeys) }
					}
				);

				return Json(new
				{
					success = true,
					message = $"Đã cập nhật {updatedCount}/{requests.Count} cấu hình!",
					updatedCount = updatedCount,
					totalRequests = requests.Count,
					failedKeys = failedKeys
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"BATCH_UPDATE",
					"SystemSettings",
					$"Exception: {ex.Message}",
					new { RequestCount = requests.Count, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		// ============================================
		// INITIALIZE DEFAULT SETTINGS
		// ============================================
		private async System.Threading.Tasks.Task InitializeDefaultSettings()
		{
			var defaultSettings = new List<SystemSetting>
			{
				// Lương
				new SystemSetting
				{
					SettingKey = "BASE_SALARY",
					SettingValue = "5000000",
					Description = "Lương cơ bản mặc định (VNĐ)",
					DataType = "Decimal",
					Category = "Salary",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "OVERTIME_RATE",
					SettingValue = "1.5",
					Description = "Hệ số lương tăng ca (x1.5)",
					DataType = "Decimal",
					Category = "Salary",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "LATE_DEDUCTION",
					SettingValue = "50000",
					Description = "Khấu trừ mỗi lần đi muộn (VNĐ)",
					DataType = "Decimal",
					Category = "Salary",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "STANDARD_HOURS_PER_DAY",
					SettingValue = "8",
					Description = "Số giờ làm chuẩn/ngày",
					DataType = "Decimal",
					Category = "Salary",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "WORK_DAYS_PER_MONTH",
					SettingValue = "26",
					Description = "Số ngày làm việc/tháng",
					DataType = "Number",
					Category = "Salary",
					IsActive = true,
					CreatedAt = DateTime.Now
				},

				// Chấm công
				new SystemSetting
				{
					SettingKey = "CHECK_IN_START_TIME",
					SettingValue = "07:00",
					Description = "Giờ bắt đầu cho phép check-in",
					DataType = "String",
					Category = "Attendance",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "CHECK_IN_STANDARD_TIME",
					SettingValue = "08:00",
					Description = "Giờ chuẩn check-in (muộn hơn = đi muộn)",
					DataType = "String",
					Category = "Attendance",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "CHECK_OUT_MIN_TIME",
					SettingValue = "17:00",
					Description = "Giờ tối thiểu check-out",
					DataType = "String",
					Category = "Attendance",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "GEOFENCE_ENABLED",
					SettingValue = "true",
					Description = "Bật kiểm tra vị trí địa lý",
					DataType = "Boolean",
					Category = "Attendance",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "GEOFENCE_RADIUS",
					SettingValue = "100",
					Description = "Bán kính cho phép (mét)",
					DataType = "Number",
					Category = "Attendance",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "OFFICE_LATITUDE",
					SettingValue = "10.7769",
					Description = "Vĩ độ văn phòng",
					DataType = "Decimal",
					Category = "Attendance",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "OFFICE_LONGITUDE",
					SettingValue = "106.7009",
					Description = "Kinh độ văn phòng",
					DataType = "Decimal",
					Category = "Attendance",
					IsActive = true,
					CreatedAt = DateTime.Now
				},

				// Chung
				new SystemSetting
				{
					SettingKey = "SYSTEM_NAME",
					SettingValue = "TMD System",
					Description = "Tên hệ thống",
					DataType = "String",
					Category = "General",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "COMPANY_NAME",
					SettingValue = "Công ty TMD",
					Description = "Tên công ty",
					DataType = "String",
					Category = "General",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "COMPANY_ADDRESS",
					SettingValue = "TP. Hồ Chí Minh",
					Description = "Địa chỉ công ty",
					DataType = "String",
					Category = "General",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "COMPANY_PHONE",
					SettingValue = "0123456789",
					Description = "Số điện thoại công ty",
					DataType = "String",
					Category = "General",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "ADMIN_EMAIL",
					SettingValue = "admin@tmd.com",
					Description = "Email admin hệ thống",
					DataType = "String",
					Category = "General",
					IsActive = true,
					CreatedAt = DateTime.Now
				},

				// Thông báo
				new SystemSetting
				{
					SettingKey = "ENABLE_EMAIL_NOTIFICATION",
					SettingValue = "false",
					Description = "Bật gửi email thông báo",
					DataType = "Boolean",
					Category = "Notification",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "ENABLE_LATE_WARNING",
					SettingValue = "true",
					Description = "Bật cảnh báo đi muộn",
					DataType = "Boolean",
					Category = "Notification",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "MAX_LATE_DAYS_PER_MONTH",
					SettingValue = "5",
					Description = "Số lần đi muộn tối đa/tháng",
					DataType = "Number",
					Category = "Notification",
					IsActive = true,
					CreatedAt = DateTime.Now
				},

				// Code Editor - Custom CSS/JS
				new SystemSetting
				{
					SettingKey = "CUSTOM_CSS",
					SettingValue = "/* Custom CSS */\n.custom-class {\n    color: #333;\n}",
					Description = "Custom CSS cho toàn hệ thống",
					DataType = "Code",
					Category = "CustomCode",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "CUSTOM_JS",
					SettingValue = "// Custom JavaScript\nconsole.log('Custom JS loaded');",
					Description = "Custom JavaScript cho toàn hệ thống",
					DataType = "Code",
					Category = "CustomCode",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "ADMIN_CUSTOM_CSS",
					SettingValue = "/* Admin Custom CSS */",
					Description = "Custom CSS cho khu vực Admin",
					DataType = "Code",
					Category = "CustomCode",
					IsActive = true,
					CreatedAt = DateTime.Now
				},
				new SystemSetting
				{
					SettingKey = "ADMIN_CUSTOM_JS",
					SettingValue = "// Admin Custom JavaScript",
					Description = "Custom JavaScript cho khu vực Admin",
					DataType = "Code",
					Category = "CustomCode",
					IsActive = true,
					CreatedAt = DateTime.Now
				}
			};

			_context.SystemSettings.AddRange(defaultSettings);
			await _context.SaveChangesAsync();

			await _auditHelper.LogAsync(
				HttpContext.Session.GetInt32("UserId"),
				"CREATE",
				"SystemSettings",
				null,
				null,
				null,
				$"Khởi tạo {defaultSettings.Count} cấu hình mặc định"
			);
		}

		// ============================================
		// REQUEST MODELS
		// ============================================
		public class UpdateSettingRequest
		{
			public string SettingKey { get; set; } = string.Empty;
			public string? SettingValue { get; set; }
		}

		// ============================================
		// HELPER METHOD: Get Setting Value by Key
		// ============================================
		public static string GetSettingValue(TmdContext context, string key)
		{
			var setting = context.SystemSettings
				.FirstOrDefault(s => s.SettingKey == key && s.IsActive == true);
			return setting?.SettingValue ?? string.Empty;
		}
	}
}