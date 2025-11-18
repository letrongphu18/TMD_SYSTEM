using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDSystem.Helpers;
using TMD.Models;
using ClosedXML.Excel;
using System.IO;

namespace TMDSystem.Controllers
{
	public class SalaryController : Controller
	{
		private readonly TmdContext _context;
		private readonly AuditHelper _auditHelper;

		public SalaryController(TmdContext context, AuditHelper auditHelper)
		{
			_context = context;
			_auditHelper = auditHelper;
		}

		private bool IsAdmin()
		{
			return HttpContext.Session.GetString("RoleName") == "Admin";
		}

		// ============================================
		// TRANG QUẢN LÝ LƯƠNG
		// ============================================
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			ViewBag.Users = await _context.Users
				.Include(u => u.Department)
				.Where(u => u.IsActive == true)
				.OrderBy(u => u.FullName)
				.ToListAsync();

			ViewBag.Departments = await _context.Departments
				.Where(d => d.IsActive == true)
				.OrderBy(d => d.DepartmentName)
				.ToListAsync();

			// Lấy cấu hình lương từ SystemSettings
			var baseSalary = await GetSettingValue("BASE_SALARY", "5000000");
			var overtimeRate = await GetSettingValue("OVERTIME_RATE", "1.5");
			var lateDeduction = await GetSettingValue("LATE_UNDER_15MIN_DEDUCTION_HOURS", "2");

			ViewBag.BaseSalary = baseSalary;
			ViewBag.OvertimeRate = overtimeRate;
			ViewBag.LateDeduction = lateDeduction;

			return View();
		}

		// ============================================
		// ✅ XEM TRƯỚC BẢNG LƯƠNG
		// ============================================
		[HttpPost]
		public async Task<IActionResult> PreviewSalary([FromBody] PreviewSalaryRequest request)
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Không có quyền truy cập!" });

			try
			{
				var fromDate = DateOnly.FromDateTime(request.FromDate);
				var toDate = DateOnly.FromDateTime(request.ToDate);

				// Lấy cấu hình
				var baseSalary = decimal.Parse(await GetSettingValue("BASE_SALARY", "5000000"));
				var overtimeRate = decimal.Parse(await GetSettingValue("OVERTIME_RATE", "1.5"));
				var standardHoursPerDay = decimal.Parse(await GetSettingValue("STANDARD_HOURS_PER_DAY", "8"));
				var workDaysPerMonth = decimal.Parse(await GetSettingValue("WORK_DAYS_PER_MONTH", "26"));

				var query = _context.Attendances
					.Include(a => a.User)
						.ThenInclude(u => u.Department)
					.Include(a => a.LateRequest)
					.Include(a => a.OvertimeRequest)
					.Where(a => a.WorkDate >= fromDate && a.WorkDate <= toDate);

				if (request.UserId.HasValue && request.UserId.Value > 0)
				{
					query = query.Where(a => a.UserId == request.UserId.Value);
				}

				if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
				{
					query = query.Where(a => a.User.DepartmentId == request.DepartmentId.Value);
				}

				var attendances = await query.ToListAsync();

				if (!attendances.Any())
				{
					return Json(new { success = false, message = "Không có dữ liệu trong khoảng thời gian này!" });
				}

				// Nhóm theo user và tính lương
				var salaryData = attendances
					.GroupBy(a => a.UserId)
					.Select(g => {
						var user = g.First().User;
						var workedDays = g.Count(a => a.CheckInTime != null && a.CheckOutTime != null);
						var lateDays = g.Count(a => a.IsLate == true);
						var totalHours = g.Sum(a => a.TotalHours ?? 0);

						// ✅ Tính deduction hours (đã được calculate trong CheckIn)
						var deductionHours = g.Sum(a => a.DeductionHours);

						// ✅ Tính overtime hours đã được approve
						var overtimeHours = g.Where(a => a.IsOvertimeApproved == true)
							.Sum(a => a.ApprovedOvertimeHours);

						// Tính lương
						var dailySalary = baseSalary / workDaysPerMonth;
						var hourlySalary = dailySalary / standardHoursPerDay;

						var actualSalary = dailySalary * workedDays;
						var overtimeSalary = hourlySalary * overtimeRate * overtimeHours;
						var deduction = hourlySalary * deductionHours;

						var totalSalary = actualSalary + overtimeSalary - deduction;

						return new
						{
							userId = user.UserId,
							fullName = user.FullName,
							departmentName = user.Department?.DepartmentName ?? "N/A",
							workedDays = workedDays,
							lateDays = lateDays,
							totalHours = totalHours,
							overtimeHours = overtimeHours,
							deductionHours = deductionHours,
							baseSalary = actualSalary,
							overtimeSalary = overtimeSalary,
							deduction = deduction,
							totalSalary = totalSalary
						};
					})
					.OrderBy(x => x.fullName)
					.ToList();

				return Json(new
				{
					success = true,
					salaryData = salaryData
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"PREVIEW",
					"Salary",
					$"Exception: {ex.Message}",
					new { Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		// ============================================
		// XUẤT EXCEL BẢNG LƯƠNG CHI TIẾT
		// ============================================
		[HttpPost]
		public async Task<IActionResult> ExportSalaryExcel([FromBody] ExportSalaryRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"EXPORT",
					"Salary",
					"Không có quyền xuất file",
					null
				);
				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");
				var fromDate = DateOnly.FromDateTime(request.FromDate);
				var toDate = DateOnly.FromDateTime(request.ToDate);

				// Lấy cấu hình
				var baseSalary = decimal.Parse(await GetSettingValue("BASE_SALARY", "5000000"));
				var overtimeRate = decimal.Parse(await GetSettingValue("OVERTIME_RATE", "1.5"));
				var standardHoursPerDay = decimal.Parse(await GetSettingValue("STANDARD_HOURS_PER_DAY", "8"));
				var workDaysPerMonth = decimal.Parse(await GetSettingValue("WORK_DAYS_PER_MONTH", "26"));

				var query = _context.Attendances
					.Include(a => a.User)
						.ThenInclude(u => u.Department)
					.Include(a => a.User)
						.ThenInclude(u => u.Role)
					.Include(a => a.LateRequest)
					.Include(a => a.OvertimeRequest)
					.Where(a => a.WorkDate >= fromDate && a.WorkDate <= toDate);

				if (request.UserId.HasValue && request.UserId.Value > 0)
				{
					query = query.Where(a => a.UserId == request.UserId.Value);
				}

				if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
				{
					query = query.Where(a => a.User.DepartmentId == request.DepartmentId.Value);
				}

				var attendances = await query
					.OrderBy(a => a.User.FullName)
					.ThenBy(a => a.WorkDate)
					.ToListAsync();

				if (!attendances.Any())
				{
					return Json(new { success = false, message = "Không có dữ liệu trong khoảng thời gian này!" });
				}

				// Tính lương theo từng người
				var salaryData = attendances
					.GroupBy(a => a.UserId)
					.Select(g => new
					{
						User = g.First().User,
						TotalDays = g.Count(),
						WorkedDays = g.Count(a => a.CheckInTime != null && a.CheckOutTime != null),
						LateDays = g.Count(a => a.IsLate == true),
						TotalHours = g.Sum(a => a.TotalHours ?? 0),
						DeductionHours = g.Sum(a => a.DeductionHours),
						OvertimeHours = g.Where(a => a.IsOvertimeApproved == true).Sum(a => a.ApprovedOvertimeHours),
						Details = g.OrderBy(a => a.WorkDate).ToList()
					})
					.ToList();

				// Tạo Excel file
				using (var workbook = new XLWorkbook())
				{
					// SHEET 1: TỔNG HỢP
					var summarySheet = workbook.Worksheets.Add("Tổng Hợp Lương");

					summarySheet.Cell(1, 1).Value = "BẢNG TỔNG HỢP LƯƠNG NHÂN VIÊN";
					summarySheet.Cell(1, 1).Style.Font.Bold = true;
					summarySheet.Cell(1, 1).Style.Font.FontSize = 16;
					summarySheet.Range(1, 1, 1, 12).Merge();
					summarySheet.Range(1, 1, 1, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

					summarySheet.Cell(2, 1).Value = $"Từ ngày: {request.FromDate:dd/MM/yyyy} - Đến ngày: {request.ToDate:dd/MM/yyyy}";
					summarySheet.Range(2, 1, 2, 12).Merge();
					summarySheet.Range(2, 1, 2, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

					// Headers
					int summaryRow = 4;
					var headers = new[] { "STT", "Mã NV", "Họ Tên", "Phòng Ban", "Số Ngày Công", "Đi Muộn",
						"Tổng Giờ", "Giờ Tăng Ca", "Giờ Bị Trừ", "Lương CB", "Lương TC", "Tổng Lương" };

					for (int i = 0; i < headers.Length; i++)
					{
						summarySheet.Cell(summaryRow, i + 1).Value = headers[i];
					}

					var headerRange = summarySheet.Range(summaryRow, 1, summaryRow, headers.Length);
					headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
					headerRange.Style.Font.FontColor = XLColor.White;
					headerRange.Style.Font.Bold = true;
					headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

					// Data
					int currentRow = summaryRow + 1;
					int stt = 1;
					decimal grandTotal = 0;

					foreach (var item in salaryData)
					{
						var dailySalary = baseSalary / workDaysPerMonth;
						var hourlySalary = dailySalary / standardHoursPerDay;

						var actualSalary = dailySalary * item.WorkedDays;
						var overtimeSalary = hourlySalary * overtimeRate * item.OvertimeHours;
						var deduction = hourlySalary * item.DeductionHours;
						var totalSalary = actualSalary + overtimeSalary - deduction;

						grandTotal += totalSalary;

						summarySheet.Cell(currentRow, 1).Value = stt++;
						summarySheet.Cell(currentRow, 2).Value = $"NV{item.User.UserId:D4}";
						summarySheet.Cell(currentRow, 3).Value = item.User.FullName;
						summarySheet.Cell(currentRow, 4).Value = item.User.Department?.DepartmentName ?? "N/A";
						summarySheet.Cell(currentRow, 5).Value = item.WorkedDays;
						summarySheet.Cell(currentRow, 6).Value = item.LateDays;
						summarySheet.Cell(currentRow, 7).Value = item.TotalHours;
						summarySheet.Cell(currentRow, 8).Value = item.OvertimeHours;
						summarySheet.Cell(currentRow, 9).Value = item.DeductionHours;
						summarySheet.Cell(currentRow, 10).Value = actualSalary;
						summarySheet.Cell(currentRow, 11).Value = overtimeSalary;
						summarySheet.Cell(currentRow, 12).Value = totalSalary;

						for (int col = 10; col <= 12; col++)
						{
							summarySheet.Cell(currentRow, col).Style.NumberFormat.Format = "#,##0";
						}

						currentRow++;
					}

					// Total row
					summarySheet.Cell(currentRow, 1).Value = "TỔNG CỘNG";
					summarySheet.Range(currentRow, 1, currentRow, 11).Merge();
					summarySheet.Cell(currentRow, 12).Value = grandTotal;
					summarySheet.Cell(currentRow, 12).Style.NumberFormat.Format = "#,##0";

					var totalRange = summarySheet.Range(currentRow, 1, currentRow, 12);
					totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7E6E6");
					totalRange.Style.Font.Bold = true;

					summarySheet.Columns().AdjustToContents();

					// Lưu file
					var fileName = $"BangLuong_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
					var memoryStream = new MemoryStream();
					workbook.SaveAs(memoryStream);
					memoryStream.Position = 0;

					await _auditHelper.LogDetailedAsync(
						adminId,
						"EXPORT",
						"Salary",
						null,
						null,
						null,
						$"Xuất bảng lương Excel từ {request.FromDate:dd/MM/yyyy} đến {request.ToDate:dd/MM/yyyy}",
						new Dictionary<string, object>
						{
							{ "TotalEmployees", salaryData.Count },
							{ "TotalSalary", grandTotal },
							{ "FileName", fileName }
						}
					);

					return File(memoryStream.ToArray(),
						"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
						fileName);
				}
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"EXPORT",
					"Salary",
					$"Exception: {ex.Message}",
					new { Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		// ============================================
		// HELPER
		// ============================================
		private async Task<string> GetSettingValue(string key, string defaultValue)
		{
			var setting = await _context.SystemSettings
				.FirstOrDefaultAsync(s => s.SettingKey == key && s.IsActive == true);

			return setting?.SettingValue ?? defaultValue;
		}

		// ============================================
		// REQUEST MODELS
		// ============================================
		public class ExportSalaryRequest
		{
			public DateTime FromDate { get; set; }
			public DateTime ToDate { get; set; }
			public int? UserId { get; set; }
			public int? DepartmentId { get; set; }
		}

		public class PreviewSalaryRequest
		{
			public DateTime FromDate { get; set; }
			public DateTime ToDate { get; set; }
			public int? UserId { get; set; }
			public int? DepartmentId { get; set; }
		}
	}
}