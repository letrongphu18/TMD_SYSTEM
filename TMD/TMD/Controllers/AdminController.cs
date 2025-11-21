using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDSystem.Helpers;
using TMD.Models;
using Microsoft.AspNetCore.Identity.Data;

namespace TMDSystem.Controllers
{
	public class AdminController : Controller
	{
		private readonly TmdContext _context;
		private readonly AuditHelper _auditHelper;

		public AdminController(TmdContext context, AuditHelper auditHelper)
		{
			_context = context;
			_auditHelper = auditHelper;
		}

		private bool IsAdmin()
		{
			return HttpContext.Session.GetString("RoleName") == "Admin";
		}

		// ============================================
		// ADMIN DASHBOARD
		// ============================================
		public async Task<IActionResult> Dashboard()
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			ViewBag.TotalUsers = await _context.Users.CountAsync();
			ViewBag.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive == true);
			ViewBag.TotalDepartments = await _context.Departments.CountAsync();

			var allTasks = await _context.Tasks
				.Include(t => t.UserTasks)
				.Where(t => t.IsActive == true)
				.ToListAsync();

			ViewBag.TotalTasks = allTasks.Count;
			ViewBag.CompletedTasks = allTasks.Count(t => t.UserTasks.Any() &&
				t.UserTasks.All(ut => ut.CompletedThisWeek >= t.TargetPerWeek));
			ViewBag.InProgressTasks = allTasks.Count - ViewBag.CompletedTasks;
			ViewBag.OverdueTasks = allTasks.Count(t => t.Deadline.HasValue && t.Deadline.Value < DateTime.Now);

			var taskCompletionRate = ViewBag.TotalTasks > 0
				? Math.Round((double)ViewBag.CompletedTasks / ViewBag.TotalTasks * 100, 1)
				: 0;
			ViewBag.TaskCompletionRate = taskCompletionRate;

			var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
			var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

			var monthlyAttendances = await _context.Attendances
				.Include(a => a.User)
				.Where(a => a.WorkDate >= DateOnly.FromDateTime(startOfMonth) &&
							a.WorkDate <= DateOnly.FromDateTime(endOfMonth))
				.ToListAsync();

			ViewBag.TotalAttendances = monthlyAttendances.Count;
			ViewBag.OnTimeCount = monthlyAttendances.Count(a => a.IsLate == false);
			ViewBag.LateCount = monthlyAttendances.Count(a => a.IsLate == true);
			ViewBag.OnTimeRate = monthlyAttendances.Count > 0
				? Math.Round((double)ViewBag.OnTimeCount / monthlyAttendances.Count * 100, 1)
				: 0;

			var topPerformers = await _context.Users
				.Include(u => u.Department)
				.Include(u => u.UserTasks)
					.ThenInclude(ut => ut.Task)
				.Where(u => u.IsActive == true && u.UserTasks.Any())
				.Select(u => new
				{
					User = u,
					TotalCompleted = u.UserTasks.Sum(ut => ut.CompletedThisWeek),
					TaskCount = u.UserTasks.Count(ut => ut.Task.IsActive == true)
				})
				.OrderByDescending(x => x.TotalCompleted)
				.Take(5)
				.ToListAsync();

			ViewBag.TopPerformers = topPerformers;

			var lateComers = await _context.Attendances
				.Include(a => a.User)
					.ThenInclude(u => u.Department)
				.Where(a => a.IsLate == true &&
							a.WorkDate >= DateOnly.FromDateTime(startOfMonth) &&
							a.WorkDate <= DateOnly.FromDateTime(endOfMonth))
				.GroupBy(a => a.UserId)
				.Select(g => new
				{
					UserId = g.Key,
					User = g.First().User,
					LateCount = g.Count()
				})
				.OrderByDescending(x => x.LateCount)
				.Take(5)
				.ToListAsync();

			ViewBag.LateComers = lateComers;

			var punctualStaff = await _context.Attendances
				.Include(a => a.User)
					.ThenInclude(u => u.Department)
				.Where(a => a.IsLate == false &&
							a.WorkDate >= DateOnly.FromDateTime(startOfMonth) &&
							a.WorkDate <= DateOnly.FromDateTime(endOfMonth))
				.GroupBy(a => a.UserId)
				.Select(g => new
				{
					UserId = g.Key,
					User = g.First().User,
					OnTimeCount = g.Count()
				})
				.OrderByDescending(x => x.OnTimeCount)
				.Take(5)
				.ToListAsync();

			ViewBag.PunctualStaff = punctualStaff;

			var tasksByPriority = allTasks
				.GroupBy(t => t.Priority ?? "Medium")
				.Select(g => new
				{
					Priority = g.Key,
					Total = g.Count(),
					Completed = g.Count(t => t.UserTasks.Any() &&
						t.UserTasks.All(ut => ut.CompletedThisWeek >= t.TargetPerWeek))
				})
				.OrderBy(x => x.Priority == "High" ? 1 : x.Priority == "Medium" ? 2 : 3)
				.ToList();

			ViewBag.TasksByPriority = tasksByPriority;

			var upcomingTasksData = await _context.Tasks
				.Include(t => t.UserTasks)
					.ThenInclude(ut => ut.User)
				.Where(t => t.IsActive == true && t.Deadline.HasValue)
				.OrderBy(t => t.Deadline)
				.Take(10)
				.ToListAsync();

			var upcomingTasks = upcomingTasksData.Select(t => new
			{
				Task = t,
				AssignedCount = t.UserTasks.Count,
				CompletedCount = t.UserTasks.Count(ut => ut.CompletedThisWeek >= t.TargetPerWeek),
				ProgressPercent = t.UserTasks.Count > 0
					? Math.Round((double)t.UserTasks.Count(ut => ut.CompletedThisWeek >= t.TargetPerWeek) / t.UserTasks.Count * 100, 1)
					: 0
			}).ToList();

			ViewBag.UpcomingTasks = upcomingTasks;

			var recentAudits = await _context.AuditLogs
				.Include(a => a.User)
				.OrderByDescending(a => a.Timestamp)
				.Take(5)
				.ToListAsync();

			ViewBag.RecentAudits = recentAudits;

			return View();
		}

		// ============================================
		// USER MANAGEMENT
		// ============================================
		public async Task<IActionResult> UserList()
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			var users = await _context.Users
				.Include(u => u.Role)
				.Include(u => u.Department)
				.OrderBy(u => u.FullName)
				.ToListAsync();

			return View(users);
		}

		[HttpPost]
		public async Task<IActionResult> ToggleUserStatus([FromBody] ToggleUserRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"User",
					"Không có quyền thực hiện",
					new { UserId = request.UserId }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			var user = await _context.Users.FindAsync(request.UserId);
			if (user == null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"User",
					"Không tìm thấy người dùng",
					new { UserId = request.UserId }
				);

				return Json(new { success = false, message = "Không tìm thấy người dùng" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");
				user.IsActive = !user.IsActive;
				user.UpdatedAt = DateTime.Now;
				await _context.SaveChangesAsync();

				await _auditHelper.LogAsync(
					adminId,
					"UPDATE",
					"User",
					user.UserId,
					new { IsActive = !user.IsActive },
					new { IsActive = user.IsActive },
					$"Admin {(user.IsActive == true ? "kích hoạt" : "vô hiệu hóa")} tài khoản: {user.Username}"
				);

				return Json(new
				{
					success = true,
					message = $"Đã {(user.IsActive == true ? "kích hoạt" : "vô hiệu hóa")} tài khoản {user.FullName}",
					isActive = user.IsActive
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"User",
					$"Exception: {ex.Message}",
					new { UserId = request.UserId, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		// ============================================
		// RESET PASSWORD
		// ============================================
		[HttpGet]
		public async Task<IActionResult> ResetUserPassword(int id)
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			var user = await _context.Users.FindAsync(id);
			if (user == null)
				return NotFound();

			ViewBag.User = user;
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> ResetUserPasswordJson([FromBody] ResetPasswordRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"PASSWORD_RESET",
					"User",
					"Không có quyền reset mật khẩu",
					new { TargetUserId = request.UserId }
				);

				return Json(new { success = false, message = "Chỉ Admin mới có quyền reset mật khẩu!" });
			}

			if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"PASSWORD_RESET",
					"User",
					"Mật khẩu không hợp lệ",
					new { UserId = request.UserId }
				);

				return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
			}

			if (string.IsNullOrEmpty(request.Reason))
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"PASSWORD_RESET",
					"User",
					"Thiếu lý do reset",
					new { UserId = request.UserId }
				);

				return Json(new { success = false, message = "Vui lòng nhập lý do reset mật khẩu" });
			}

			var user = await _context.Users.FindAsync(request.UserId);
			if (user == null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"PASSWORD_RESET",
					"User",
					"User không tồn tại",
					new { UserId = request.UserId }
				);

				return Json(new { success = false, message = "Không tìm thấy người dùng" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");
				var oldHash = user.PasswordHash;

				user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
				user.UpdatedAt = DateTime.Now;

				var resetHistory = new PasswordResetHistory
				{
					UserId = user.UserId,
					ResetByUserId = adminId,
					OldPasswordHash = oldHash,
					ResetTime = DateTime.Now,
					ResetReason = request.Reason,
					Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
				};

				_context.PasswordResetHistories.Add(resetHistory);
				await _context.SaveChangesAsync();

				await _auditHelper.LogAsync(
					adminId,
					"PASSWORD_RESET",
					"User",
					user.UserId,
					null,
					null,
					$"Admin reset mật khẩu cho user: {user.Username}. Lý do: {request.Reason}"
				);

				return Json(new
				{
					success = true,
					message = $"Reset mật khẩu thành công cho {user.FullName}!"
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"PASSWORD_RESET",
					"User",
					$"Exception: {ex.Message}",
					new { UserId = request.UserId, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetAllUsers()
		{
			try
			{
				var users = await _context.Users
					.Include(u => u.Department)
					.Where(u => u.IsActive == true)
					.OrderBy(u => u.FullName)
					.Select(u => new
					{
						userId = u.UserId,
						fullName = u.FullName,
						email = u.Email,
						departmentName = u.Department != null ? u.Department.DepartmentName : "N/A"
					})
					.ToListAsync();

				return Json(new { success = true, users = users });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// ============================================
		// PASSWORD RESET HISTORY
		// ============================================
		public async Task<IActionResult> PasswordResetHistory()
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			await _auditHelper.LogViewAsync(
				HttpContext.Session.GetInt32("UserId").Value,
				"PasswordResetHistory",
				0,
				"Xem lịch sử reset mật khẩu"
			);

			var history = await _context.PasswordResetHistories
				.Include(p => p.User)
				.Include(p => p.ResetByUser)
				.OrderByDescending(p => p.ResetTime)
				.ToListAsync();

			return View(history);
		}

		// ============================================
		// ATTENDANCE MANAGEMENT
		// ============================================
		public async Task<IActionResult> AttendanceList(DateTime? date)
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			var selectedDate = date ?? DateTime.Today;

			await _auditHelper.LogViewAsync(
				HttpContext.Session.GetInt32("UserId").Value,
				"Attendance",
				0,
				$"Xem danh sách chấm công ngày {selectedDate:dd/MM/yyyy}"
			);

			var attendances = await _context.Attendances
				.Include(a => a.User)
					.ThenInclude(u => u.Department)
				.Where(a => a.WorkDate == DateOnly.FromDateTime(selectedDate))
				.OrderByDescending(a => a.CheckInTime)
				.ToListAsync();

			ViewBag.SelectedDate = selectedDate;
			return View(attendances);
		}

		public async Task<IActionResult> AttendanceHistory(int? userId, DateTime? fromDate, DateTime? toDate, int? departmentId)
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			var from = fromDate ?? DateTime.Today.AddDays(-30);
			var to = toDate ?? DateTime.Today;

			await _auditHelper.LogDetailedAsync(
				HttpContext.Session.GetInt32("UserId").Value,
				"VIEW",
				"Attendance",
				null,
				null,
				null,
				"Xem lịch sử chấm công tổng hợp",
				new Dictionary<string, object>
				{
					{ "FilterUserId", userId ?? 0 },
					{ "FilterDepartment", departmentId ?? 0 },
					{ "FromDate", from.ToString("yyyy-MM-dd") },
					{ "ToDate", to.ToString("yyyy-MM-dd") }
				}
			);

			var query = _context.Attendances
				.Include(a => a.User)
					.ThenInclude(u => u.Department)
				.AsQueryable();

			if (userId.HasValue && userId.Value > 0)
			{
				query = query.Where(a => a.UserId == userId.Value);
			}

			if (departmentId.HasValue && departmentId.Value > 0)
			{
				query = query.Where(a => a.User.DepartmentId == departmentId.Value);
			}

			query = query.Where(a =>
				a.WorkDate >= DateOnly.FromDateTime(from) &&
				a.WorkDate <= DateOnly.FromDateTime(to)
			);

			var attendances = await query
				.OrderByDescending(a => a.WorkDate)
				.ThenByDescending(a => a.CheckInTime)
				.ToListAsync();

			ViewBag.TotalRecords = attendances.Count;
			ViewBag.TotalCheckIns = attendances.Count(a => a.CheckInTime != null);
			ViewBag.TotalCheckOuts = attendances.Count(a => a.CheckOutTime != null);
			ViewBag.CompletedDays = attendances.Count(a => a.CheckInTime != null && a.CheckOutTime != null);
			ViewBag.OnTimeCount = attendances.Count(a => a.IsLate == false);
			ViewBag.LateCount = attendances.Count(a => a.IsLate == true);
			ViewBag.TotalWorkHours = attendances.Sum(a => a.TotalHours ?? 0);
			ViewBag.WithinGeofence = attendances.Count(a => a.IsWithinGeofence == true);
			ViewBag.OutsideGeofence = attendances.Count(a => a.IsWithinGeofence == false);

			ViewBag.Users = await _context.Users
				.Where(u => u.IsActive == true)
				.OrderBy(u => u.FullName)
				.ToListAsync();

			ViewBag.Departments = await _context.Departments
				.OrderBy(d => d.DepartmentName)
				.ToListAsync();

			ViewBag.SelectedUserId = userId;
			ViewBag.SelectedDepartmentId = departmentId;
			ViewBag.FromDate = from;
			ViewBag.ToDate = to;

			return View(attendances);
		}

		// ============================================
		// AUDIT LOGS (FIXED: no StringComparison)
		// ============================================
		public async Task<IActionResult> AuditLogs(string? action, DateTime? fromDate, DateTime? toDate)
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			await _auditHelper.LogViewAsync(
				HttpContext.Session.GetInt32("UserId").Value,
				"AuditLog",
				0,
				$"Xem nhật ký hoạt động - Filter: {action ?? "All"}"
			);

			var query = _context.AuditLogs
				.Include(a => a.User)
				.AsQueryable();

			// Case-insensitive filter without StringComparison (EF-friendly)
			if (!string.IsNullOrWhiteSpace(action))
			{
				var act = action.Trim().ToLower();
				query = query.Where(a => a.Action != null && a.Action.ToLower() == act);
			}

			// Normalize date range (swap if inverted)
			if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
			{
				var t = fromDate; fromDate = toDate; toDate = t;
			}

			// Inclusive end date (to midnight next day)
			if (fromDate.HasValue)
				query = query.Where(a => a.Timestamp.HasValue && a.Timestamp.Value >= fromDate.Value);

			if (toDate.HasValue)
				query = query.Where(a => a.Timestamp.HasValue && a.Timestamp.Value < toDate.Value.Date.AddDays(1));

			var logs = await query
				.OrderByDescending(a => a.Timestamp)
				.Take(1000)
				.ToListAsync();

			// Fallback to recent when filters yield none
			if (!logs.Any())
			{
				logs = await _context.AuditLogs
					.Include(a => a.User)
					.OrderByDescending(a => a.Timestamp)
					.Take(20)
					.ToListAsync();

				TempData["Info"] = "Không có log theo bộ lọc. Đang hiển thị gần đây.";
			}

			ViewBag.Actions = await _context.AuditLogs
				.Where(a => a.Action != null)
				.Select(a => a.Action)
				.Distinct()
				.OrderBy(a => a)
				.ToListAsync();

			ViewBag.SelectedAction = action;
			ViewBag.FromDate = fromDate;
			ViewBag.ToDate = toDate;

			return View(logs);
		}

		// ============================================
		// LOGIN HISTORY
		// ============================================
		public async Task<IActionResult> LoginHistory(DateTime? fromDate, DateTime? toDate, bool? isSuccess)
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			await _auditHelper.LogDetailedAsync(
				HttpContext.Session.GetInt32("UserId").Value,
				"VIEW",
				"LoginHistory",
				null,
				null,
				null,
				"Xem lịch sử đăng nhập hệ thống",
				new Dictionary<string, object>
				{
					{ "FromDate", fromDate?.ToString("yyyy-MM-dd") ?? "All" },
					{ "ToDate", toDate?.ToString("yyyy-MM-dd") ?? "All" },
					{ "FilterSuccess", isSuccess?.ToString() ?? "All" }
				}
			);

			var query = _context.LoginHistories
				.Include(l => l.User)
				.AsQueryable();

			if (fromDate.HasValue)
				query = query.Where(l => l.LoginTime.HasValue && l.LoginTime.Value >= fromDate.Value);

			if (toDate.HasValue)
				query = query.Where(l => l.LoginTime.HasValue && l.LoginTime.Value <= toDate.Value.AddDays(1));

			if (isSuccess.HasValue)
				query = query.Where(l => l.IsSuccess == isSuccess.Value);

			var history = await query
				.OrderByDescending(l => l.LoginTime)
				.Take(1000)
				.ToListAsync();

			ViewBag.FromDate = fromDate;
			ViewBag.ToDate = toDate;
			ViewBag.IsSuccess = isSuccess;

			return View(history);
		}

		// ============================================
		// DEPARTMENT MANAGEMENT
		// ============================================
		public async Task<IActionResult> DepartmentList()
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			var departments = await _context.Departments
				.Include(d => d.Users)
				.OrderBy(d => d.DepartmentName)
				.ToListAsync();

			return View(departments);
		}

		[HttpGet]
		public async Task<IActionResult> DepartmentDetail(int id)
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			await _auditHelper.LogViewAsync(
				HttpContext.Session.GetInt32("UserId").Value,
				"Department",
				id,
				"Xem chi tiết phòng ban"
			);

			var department = await _context.Departments
				.Include(d => d.Users)
					.ThenInclude(u => u.Role)
				.FirstOrDefaultAsync(d => d.DepartmentId == id);

			if (department == null)
				return NotFound();

			return View(department);
		}

		[HttpPost]
		public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"CREATE",
					"Department",
					"Không có quyền tạo phòng ban",
					new { DepartmentName = request.DepartmentName }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			if (string.IsNullOrWhiteSpace(request.DepartmentName))
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"CREATE",
					"Department",
					"Tên phòng ban rỗng",
					null
				);

				return Json(new { success = false, message = "Tên phòng ban không được để trống!" });
			}

			var existingDept = await _context.Departments
				.FirstOrDefaultAsync(d => d.DepartmentName.ToLower() == request.DepartmentName.Trim().ToLower());

			if (existingDept != null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"CREATE",
					"Department",
					"Tên phòng ban đã tồn tại",
					new { DepartmentName = request.DepartmentName }
				);

				return Json(new { success = false, message = "Tên phòng ban đã tồn tại!" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");

				var department = new Department
				{
					DepartmentName = request.DepartmentName.Trim(),
					Description = request.Description?.Trim(),
					IsActive = request.IsActive,
					CreatedAt = DateTime.Now,
					UpdatedAt = DateTime.Now
				};

				_context.Departments.Add(department);
				await _context.SaveChangesAsync();

				await _auditHelper.LogAsync(
					adminId,
					"CREATE",
					"Department",
					department.DepartmentId,
					null,
					new { department.DepartmentName, department.Description, department.IsActive },
					$"Tạo phòng ban mới: {department.DepartmentName}"
				);

				return Json(new
				{
					success = true,
					message = $"Tạo phòng ban '{department.DepartmentName}' thành công!",
					departmentId = department.DepartmentId
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"CREATE",
					"Department",
					$"Exception: {ex.Message}",
					new { DepartmentName = request.DepartmentName, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		[HttpPost]
		public async Task<IActionResult> UpdateDepartment([FromBody] UpdateDepartmentRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Department",
					"Không có quyền cập nhật",
					new { DepartmentId = request.DepartmentId }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			if (string.IsNullOrWhiteSpace(request.DepartmentName))
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Department",
					"Tên phòng ban rỗng",
					new { DepartmentId = request.DepartmentId }
				);

				return Json(new { success = false, message = "Tên phòng ban không được để trống!" });
			}

			var department = await _context.Departments.FindAsync(request.DepartmentId);

			if (department == null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Department",
					"Phòng ban không tồn tại",
					new { DepartmentId = request.DepartmentId }
				);

				return Json(new { success = false, message = "Không tìm thấy phòng ban!" });
			}

			var existingDept = await _context.Departments
				.FirstOrDefaultAsync(d => d.DepartmentId != request.DepartmentId &&
										  d.DepartmentName.ToLower() == request.DepartmentName.Trim().ToLower());

			if (existingDept != null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Department",
					"Tên phòng ban đã tồn tại",
					new { DepartmentId = request.DepartmentId, DepartmentName = request.DepartmentName }
				);

				return Json(new { success = false, message = "Tên phòng ban đã tồn tại!" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");

				var oldValues = new
				{
					department.DepartmentName,
					department.Description,
					department.IsActive
				};

				department.DepartmentName = request.DepartmentName.Trim();
				department.Description = request.Description?.Trim();
				department.IsActive = request.IsActive;
				department.UpdatedAt = DateTime.Now;

				await _context.SaveChangesAsync();

				var newValues = new
				{
					department.DepartmentName,
					department.Description,
					department.IsActive
				};

				await _auditHelper.LogAsync(
					adminId,
					"UPDATE",
					"Department",
					department.DepartmentId,
					oldValues,
					newValues,
					$"Cập nhật phòng ban: {department.DepartmentName}"
				);

				return Json(new
				{
					success = true,
					message = $"Cập nhật phòng ban '{department.DepartmentName}' thành công!"
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Department",
					$"Exception: {ex.Message}",
					new { DepartmentId = request.DepartmentId, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		[HttpPost]
		public async Task<IActionResult> ToggleDepartmentStatus([FromBody] ToggleDepartmentRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Department",
					"Không có quyền thực hiện",
					new { DepartmentId = request.DepartmentId }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			var department = await _context.Departments
				.Include(d => d.Users)
				.FirstOrDefaultAsync(d => d.DepartmentId == request.DepartmentId);

			if (department == null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Department",
					"Phòng ban không tồn tại",
					new { DepartmentId = request.DepartmentId }
				);

				return Json(new { success = false, message = "Không tìm thấy phòng ban!" });
			}

			if (department.IsActive == true && department.Users != null && department.Users.Any(u => u.IsActive == true))
			{
				var activeUserCount = department.Users.Count(u => u.IsActive == true);
				if (activeUserCount > 0)
				{
					await _auditHelper.LogFailedAttemptAsync(
						HttpContext.Session.GetInt32("UserId"),
						"UPDATE",
						"Department",
						"Phòng ban có nhân viên đang hoạt động",
						new { DepartmentId = request.DepartmentId, ActiveUsers = activeUserCount }
					);

					return Json(new
					{
						success = false,
						message = $"Không thể vô hiệu hóa phòng ban có {activeUserCount} nhân viên đang hoạt động!"
					});
				}
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");

				department.IsActive = !department.IsActive;
				department.UpdatedAt = DateTime.Now;

				await _context.SaveChangesAsync();

				await _auditHelper.LogAsync(
					adminId,
					"UPDATE",
					"Department",
					department.DepartmentId,
					new { IsActive = !department.IsActive },
					new { IsActive = department.IsActive },
					$"Thay đổi trạng thái phòng ban: {department.DepartmentName} - {(department.IsActive == true ? "Kích hoạt" : "Vô hiệu hóa")}"
				);

				return Json(new
				{
					success = true,
					message = $"Đã {(department.IsActive == true ? "kích hoạt" : "vô hiệu hóa")} phòng ban: {department.DepartmentName}",
					isActive = department.IsActive
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Department",
					$"Exception: {ex.Message}",
					new { DepartmentId = request.DepartmentId, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		[HttpPost]
		public async Task<IActionResult> DeleteDepartment([FromBody] DeleteDepartmentRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"DELETE",
					"Department",
					"Không có quyền xóa",
					new { DepartmentId = request.DepartmentId }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			var department = await _context.Departments
				.Include(d => d.Users)
				.FirstOrDefaultAsync(d => d.DepartmentId == request.DepartmentId);

			if (department == null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"DELETE",
					"Department",
					"Phòng ban không tồn tại",
					new { DepartmentId = request.DepartmentId }
				);

				return Json(new { success = false, message = "Không tìm thấy phòng ban!" });
			}

			if (department.Users != null && department.Users.Any())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"DELETE",
					"Department",
					"Phòng ban có nhân viên",
					new { DepartmentId = request.DepartmentId, UserCount = department.Users.Count }
				);

				return Json(new
				{
					success = false,
					message = $"Không thể xóa phòng ban có {department.Users.Count} nhân viên! Vui lòng chuyển họ sang phòng ban khác trước."
				});
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");

				department.IsActive = false;
				department.UpdatedAt = DateTime.Now;

				await _context.SaveChangesAsync();

				await _auditHelper.LogAsync(
					adminId,
					"DELETE",
					"Department",
					department.DepartmentId,
					new { IsActive = true },
					new { IsActive = false },
					$"Xóa phòng ban: {department.DepartmentName}"
				);

				return Json(new
				{
					success = true,
					message = $"Đã xóa phòng ban: {department.DepartmentName}"
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"DELETE",
					"Department",
					$"Exception: {ex.Message}",
					new { DepartmentId = request.DepartmentId, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetDepartmentDetails(int id)
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Không có quyền truy cập!" });

			await _auditHelper.LogViewAsync(
				HttpContext.Session.GetInt32("UserId").Value,
				"Department",
				id,
				"Xem chi tiết phòng ban (AJAX)"
			);

			var department = await _context.Departments
				.Include(d => d.Users)
					.ThenInclude(u => u.Role)
				.FirstOrDefaultAsync(d => d.DepartmentId == id);

			if (department == null)
				return Json(new { success = false, message = "Không tìm thấy phòng ban!" });

			var result = new
			{
				success = true,
				department = new
				{
					department.DepartmentId,
					department.DepartmentName,
					department.Description,
					department.IsActive,
					department.CreatedAt,
					department.UpdatedAt,
					TotalUsers = department.Users?.Count ?? 0,
					ActiveUsers = department.Users?.Count(u => u.IsActive == true) ?? 0,
					InactiveUsers = department.Users?.Count(u => u.IsActive == false) ?? 0,
					Users = department.Users?.Select(u => new
					{
						u.UserId,
						u.Username,
						u.FullName,
						u.Email,
						u.Avatar,
						RoleName = u.Role?.RoleName,
						u.IsActive
					}).OrderBy(u => u.FullName).ToList()
				}
			};

			return Json(result);
		}

		// ============================================
		// TASK MANAGEMENT - CRUD
		// ============================================
		public async Task<IActionResult> TaskList()
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			var tasks = await _context.Tasks
				.Include(t => t.UserTasks)
					.ThenInclude(ut => ut.User)
				.OrderByDescending(t => t.CreatedAt)
				.ToListAsync();

			ViewBag.TotalTasks = tasks.Count;
			ViewBag.ActiveTasks = tasks.Count(t => t.IsActive == true);
			ViewBag.InactiveTasks = tasks.Count(t => t.IsActive == false);
			ViewBag.OverdueTasks = tasks.Count(t => t.Deadline.HasValue && t.Deadline.Value < DateTime.Now);

			return View(tasks);
		}

		[HttpGet]
		public async Task<IActionResult> CreateTask()
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			ViewBag.Users = await _context.Users
				.Include(u => u.Department)
				.Where(u => u.IsActive == true)
				.OrderBy(u => u.FullName)
				.ToListAsync();

			return View();
		}

		[HttpPost]
		public async Task<IActionResult> CreateTaskPost([FromBody] CreateTaskRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"CREATE",
					"Task",
					"Không có quyền tạo task",
					new { TaskName = request.TaskName }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			if (string.IsNullOrWhiteSpace(request.TaskName))
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"CREATE",
					"Task",
					"Tên task rỗng",
					null
				);

				return Json(new { success = false, message = "Tên task không được để trống!" });
			}

			if (request.TargetPerWeek < 0)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"CREATE",
					"Task",
					"Target không hợp lệ",
					new { TaskName = request.TaskName, Target = request.TargetPerWeek }
				);

				return Json(new { success = false, message = "Target phải >= 0!" });
			}

			if (request.Deadline.HasValue && request.Deadline.Value < DateTime.Now)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"CREATE",
					"Task",
					"Deadline không hợp lệ",
					new { TaskName = request.TaskName, Deadline = request.Deadline }
				);

				return Json(new { success = false, message = "Deadline không được là thời điểm trong quá khứ!" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");

				var task = new TMD.Models.Task
				{
					TaskName = request.TaskName.Trim(),
					Description = request.Description?.Trim(),
					Platform = request.Platform?.Trim(),
					TargetPerWeek = request.TargetPerWeek,
					Deadline = request.Deadline,
					Priority = request.Priority ?? "Medium",
					IsActive = true,
					CreatedAt = DateTime.Now,
					UpdatedAt = DateTime.Now
				};

				_context.Tasks.Add(task);
				await _context.SaveChangesAsync();

				if (request.AssignedUserIds != null && request.AssignedUserIds.Count > 0)
				{
					foreach (var userId in request.AssignedUserIds)
					{
						var userTask = new UserTask
						{
							UserId = userId,
							TaskId = task.TaskId,
							CompletedThisWeek = 0,
							WeekStartDate = DateOnly.FromDateTime(DateTime.Today),
							CreatedAt = DateTime.Now,
							UpdatedAt = DateTime.Now
						};
						_context.UserTasks.Add(userTask);
					}
					await _context.SaveChangesAsync();
				}

				await _auditHelper.LogDetailedAsync(
					adminId,
					"CREATE",
					"Task",
					task.TaskId,
					null,
					new { task.TaskName, task.Platform, task.Priority, task.TargetPerWeek },
					$"Tạo task mới: {task.TaskName}",
					new Dictionary<string, object>
					{
						{ "AssignedUsers", request.AssignedUserIds?.Count ?? 0 },
						{ "CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
					}
				);

				return Json(new
				{
					success = true,
					message = "Tạo task thành công!",
					taskId = task.TaskId
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"CREATE",
					"Task",
					$"Exception: {ex.Message}",
					new { TaskName = request.TaskName, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		[HttpGet]
		public async Task<IActionResult> EditTask(int id)
		{
			if (!IsAdmin())
				return RedirectToAction("Login", "Account");

			var task = await _context.Tasks
				.Include(t => t.UserTasks)
					.ThenInclude(ut => ut.User)
						.ThenInclude(u => u.Department)
				.FirstOrDefaultAsync(t => t.TaskId == id);

			if (task == null)
				return NotFound();

			ViewBag.Users = await _context.Users
				.Include(u => u.Department)
				.Where(u => u.IsActive == true)
				.OrderBy(u => u.FullName)
				.ToListAsync();

			ViewBag.AssignedUserIds = task.UserTasks.Select(ut => ut.UserId).ToList();

			return View(task);
		}

		[HttpPost]
		public async Task<IActionResult> UpdateTask([FromBody] UpdateTaskRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Task",
					"Không có quyền cập nhật",
					new { TaskId = request.TaskId }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			if (string.IsNullOrWhiteSpace(request.TaskName))
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Task",
					"Tên task rỗng",
					new { TaskId = request.TaskId }
				);

				return Json(new { success = false, message = "Tên task không được để trống!" });
			}

			if (request.TargetPerWeek < 0)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Task",
					"Target không hợp lệ",
					new { TaskId = request.TaskId, Target = request.TargetPerWeek }
				);

				return Json(new { success = false, message = "Target phải >= 0!" });
			}

			var task = await _context.Tasks
				.Include(t => t.UserTasks)
				.FirstOrDefaultAsync(t => t.TaskId == request.TaskId);

			if (task == null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Task",
					"Task không tồn tại",
					new { TaskId = request.TaskId }
				);

				return Json(new { success = false, message = "Không tìm thấy task!" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");

				var oldValues = new
				{
					task.TaskName,
					task.Description,
					task.Platform,
					task.TargetPerWeek,
					task.Deadline,
					task.Priority
				};

				task.TaskName = request.TaskName.Trim();
				task.Description = request.Description?.Trim();
				task.Platform = request.Platform?.Trim();
				task.TargetPerWeek = request.TargetPerWeek;
				task.Deadline = request.Deadline;
				task.Priority = request.Priority ?? "Medium";
				task.UpdatedAt = DateTime.Now;

				var oldAssignments = task.UserTasks.ToList();
				_context.UserTasks.RemoveRange(oldAssignments);

				if (request.AssignedUserIds != null && request.AssignedUserIds.Count > 0)
				{
					foreach (var userId in request.AssignedUserIds)
					{
						var userTask = new UserTask
						{
							UserId = userId,
							TaskId = task.TaskId,
							CompletedThisWeek = 0,
							WeekStartDate = DateOnly.FromDateTime(DateTime.Today),
							CreatedAt = DateTime.Now,
							UpdatedAt = DateTime.Now
						};
						_context.UserTasks.Add(userTask);
					}
				}

				await _context.SaveChangesAsync();

				var newValues = new
				{
					task.TaskName,
					task.Description,
					task.Platform,
					task.TargetPerWeek,
					task.Deadline,
					task.Priority
				};

				await _auditHelper.LogAsync(
					adminId,
					"UPDATE",
					"Task",
					task.TaskId,
					oldValues,
					newValues,
					$"Cập nhật task: {task.TaskName}"
				);

				return Json(new
				{
					success = true,
					message = "Cập nhật task thành công!"
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Task",
					$"Exception: {ex.Message}",
					new { TaskId = request.TaskId, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		[HttpPost]
		public async Task<IActionResult> DeleteTask([FromBody] DeleteTaskRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"DELETE",
					"Task",
					"Không có quyền xóa",
					new { TaskId = request.TaskId }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			var task = await _context.Tasks.FindAsync(request.TaskId);

			if (task == null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"DELETE",
					"Task",
					"Task không tồn tại",
					new { TaskId = request.TaskId }
				);

				return Json(new { success = false, message = "Không tìm thấy task!" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");

				task.IsActive = false;
				task.UpdatedAt = DateTime.Now;

				await _context.SaveChangesAsync();

				await _auditHelper.LogAsync(
					adminId,
					"DELETE",
					"Task",
					task.TaskId,
					new { IsActive = true },
					new { IsActive = false },
					$"Xóa task: {task.TaskName}"
				);

				return Json(new
				{
					success = true,
					message = $"Đã xóa task: {task.TaskName}"
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"DELETE",
					"Task",
					$"Exception: {ex.Message}",
					new { TaskId = request.TaskId, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		[HttpPost]
		public async Task<IActionResult> ToggleTaskStatus([FromBody] ToggleTaskStatusRequest request)
		{
			if (!IsAdmin())
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Task",
					"Không có quyền thực hiện",
					new { TaskId = request.TaskId }
				);

				return Json(new { success = false, message = "Không có quyền thực hiện!" });
			}

			var task = await _context.Tasks.FindAsync(request.TaskId);

			if (task == null)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Task",
					"Task không tồn tại",
					new { TaskId = request.TaskId }
				);

				return Json(new { success = false, message = "Không tìm thấy task!" });
			}

			try
			{
				var adminId = HttpContext.Session.GetInt32("UserId");

				task.IsActive = !task.IsActive;
				task.UpdatedAt = DateTime.Now;

				await _context.SaveChangesAsync();

				await _auditHelper.LogAsync(
					adminId,
					"UPDATE",
					"Task",
					task.TaskId,
					new { IsActive = !task.IsActive },
					new { IsActive = task.IsActive },
					$"Thay đổi trạng thái task: {task.TaskName} - {(task.IsActive == true ? "Kích hoạt" : "Vô hiệu hóa")}"
				);

				return Json(new
				{
					success = true,
					message = $"Đã {(task.IsActive == true ? "kích hoạt" : "vô hiệu hóa")} task: {task.TaskName}",
					isActive = task.IsActive
				});
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(
					HttpContext.Session.GetInt32("UserId"),
					"UPDATE",
					"Task",
					$"Exception: {ex.Message}",
					new { TaskId = request.TaskId, Error = ex.ToString() }
				);

				return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetTaskDetails(int id)
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Không có quyền truy cập!" });

			await _auditHelper.LogViewAsync(
				HttpContext.Session.GetInt32("UserId").Value,
				"Task",
				id,
				"Xem chi tiết task"
			);

			var task = await _context.Tasks
				.Include(t => t.UserTasks)
					.ThenInclude(ut => ut.User)
						.ThenInclude(u => u.Department)
				.FirstOrDefaultAsync(t => t.TaskId == id);

			if (task == null)
				return Json(new { success = false, message = "Không tìm thấy task!" });

			var result = new
			{
				success = true,
				task = new
				{
					task.TaskId,
					task.TaskName,
					task.Description,
					task.Platform,
					task.TargetPerWeek,
					task.Deadline,
					task.Priority,
					task.IsActive,
					task.CreatedAt,
					task.UpdatedAt,
					AssignedUsers = task.UserTasks.Select(ut => new
					{
						ut.User.UserId,
						ut.User.FullName,
						ut.User.Avatar,
						DepartmentName = ut.User.Department?.DepartmentName,
						ut.CompletedThisWeek,
						ut.ReportLink
					}).ToList()
				}
			};

			return Json(result);
		}
		// Thêm method này vào AdminController.cs
		// Thay thế cả 2 method này

		[HttpGet]
		public async Task<JsonResult> GetPendingRequests(string? type, string? status, string? from, string? to, string? keyword)
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Không có quyền truy cập" });

			try
			{
				// Parse dates
				DateTime? fromDate = string.IsNullOrEmpty(from) ? null : DateTime.Parse(from);
				DateTime? toDate = string.IsNullOrEmpty(to) ? null : DateTime.Parse(to);

				var overtime = new List<object>();
				var leave = new List<object>();
				var late = new List<object>();

				// Overtime Requests
				if (string.IsNullOrEmpty(type) || type == "Overtime")
				{
					var otQuery = _context.OvertimeRequests
						.Include(x => x.User)
						.AsQueryable();

					if (!string.IsNullOrEmpty(status))
						otQuery = otQuery.Where(x => x.Status == status);

					if (fromDate.HasValue)
						otQuery = otQuery.Where(x => x.CreatedAt >= fromDate.Value);

					if (toDate.HasValue)
						otQuery = otQuery.Where(x => x.CreatedAt <= toDate.Value.AddDays(1));

					if (!string.IsNullOrEmpty(keyword))
					{
						var kw = keyword.Trim().ToLower();
						otQuery = otQuery.Where(x =>
							(x.Reason ?? "").ToLower().Contains(kw) ||
							(x.TaskDescription ?? "").ToLower().Contains(kw) ||
							(x.User.FullName ?? "").ToLower().Contains(kw)
						);
					}

					overtime = await otQuery
						.OrderByDescending(x => x.CreatedAt)
						.Select(x => new
						{
							x.OvertimeRequestId,
							x.UserId,
							employeeId = x.User.Username,
							employeeName = x.User.FullName,
							workDate = x.WorkDate.ToString("yyyy-MM-dd"),
							actualCheckOutTime = x.ActualCheckOutTime != null ? ((DateTime)x.ActualCheckOutTime).ToString("HH:mm:ss") : "N/A",
							overtimeHours = x.OvertimeHours,
							reason = x.Reason ?? "",
							taskDescription = x.TaskDescription ?? "",
							status = x.Status ?? "",
							createdAt = x.CreatedAt.HasValue ? x.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : ""
						})
						.ToListAsync<object>();
				}

				// Leave Requests
				if (string.IsNullOrEmpty(type) || type == "Leave")
				{
					var leaveQuery = _context.LeaveRequests
						.Include(x => x.User)
						.AsQueryable();

					if (!string.IsNullOrEmpty(status))
						leaveQuery = leaveQuery.Where(x => x.Status == status);

					if (fromDate.HasValue)
						leaveQuery = leaveQuery.Where(x => x.CreatedAt >= fromDate.Value);

					if (toDate.HasValue)
						leaveQuery = leaveQuery.Where(x => x.CreatedAt <= toDate.Value.AddDays(1));

					if (!string.IsNullOrEmpty(keyword))
					{
						var kw = keyword.Trim().ToLower();
						leaveQuery = leaveQuery.Where(x =>
							(x.Reason ?? "").ToLower().Contains(kw) ||
							(x.ProofDocument ?? "").ToLower().Contains(kw) ||
							(x.User.FullName ?? "").ToLower().Contains(kw)
						);
					}

					leave = await leaveQuery
						.OrderByDescending(x => x.CreatedAt)
						.Select(x => new
						{
							x.LeaveRequestId,
							x.UserId,
							employeeId = x.User.Username,
							employeeName = x.User.FullName,
							leaveType = x.LeaveType ?? "",
							startDate = x.StartDate.ToString("yyyy-MM-dd"),
							endDate = x.EndDate.ToString("yyyy-MM-dd"),
							totalDays = x.TotalDays,
							reason = x.Reason ?? "",
							proofDocument = x.ProofDocument ?? "",
							status = x.Status ?? "",
							createdAt = x.CreatedAt.HasValue ? x.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : ""
						})
						.ToListAsync<object>();
				}

				// Late Requests
				if (string.IsNullOrEmpty(type) || type == "Late")
				{
					var lateQuery = _context.LateRequests
						.Include(x => x.User)
						.AsQueryable();

					if (!string.IsNullOrEmpty(status))
						lateQuery = lateQuery.Where(x => x.Status == status);

					if (fromDate.HasValue)
						lateQuery = lateQuery.Where(x => x.CreatedAt >= fromDate.Value);

					if (toDate.HasValue)
						lateQuery = lateQuery.Where(x => x.CreatedAt <= toDate.Value.AddDays(1));

					if (!string.IsNullOrEmpty(keyword))
					{
						var kw = keyword.Trim().ToLower();
						lateQuery = lateQuery.Where(x =>
							(x.Reason ?? "").ToLower().Contains(kw) ||
							(x.ProofDocument ?? "").ToLower().Contains(kw) ||
							(x.User.FullName ?? "").ToLower().Contains(kw)
						);
					}

					late = await lateQuery
						.OrderByDescending(x => x.CreatedAt)
						.Select(x => new
						{
							x.LateRequestId,
							x.UserId,
							employeeId = x.User.Username,
							employeeName = x.User.FullName,
							requestDate = x.RequestDate.ToString("yyyy-MM-dd"),
							expectedArrivalTime = x.ExpectedArrivalTime.ToString("HH:mm"),
							reason = x.Reason ?? "",
							proofDocument = x.ProofDocument ?? "",
							status = x.Status ?? "",
							createdAt = x.CreatedAt.HasValue ? x.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : ""
						})
						.ToListAsync<object>();
				}

				return Json(new { success = true, overtime, leave, late });
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"GetPendingRequests Error: {ex.Message}\n{ex.StackTrace}");
				return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
			}
		}

		[HttpGet]
		public async Task<JsonResult> GetRequestDetail(string type, int id)
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Không có quyền" });

			try
			{
				if (type == "Overtime")
				{
					var r = await _context.OvertimeRequests
						.Include(x => x.User)
						.FirstOrDefaultAsync(x => x.OvertimeRequestId == id);

					if (r == null)
						return Json(new { success = false, message = "Không tìm thấy request" });

					// Xử lý ActualCheckOutTime an toàn
					string checkOutTimeStr = "N/A";
					if (r.ActualCheckOutTime != null && r.ActualCheckOutTime != default(DateTime))
					{
						checkOutTimeStr = ((DateTime)r.ActualCheckOutTime).ToString("HH:mm:ss");
					}

					return Json(new
					{
						success = true,
						request = new
						{
							overtimeRequestId = r.OvertimeRequestId,
							userId = r.UserId,
							employeeName = r.User?.FullName ?? "N/A",
							workDate = r.WorkDate.ToString("dd/MM/yyyy"),
							actualCheckOutTime = checkOutTimeStr,
							overtimeHours = r.OvertimeHours,
							reason = r.Reason ?? "",
							taskDescription = r.TaskDescription ?? "",
							status = r.Status ?? "Pending",
							reviewedBy = r.ReviewedBy ?? 0,
							reviewedAt = r.ReviewedAt?.ToString("dd/MM/yyyy HH:mm") ?? "",
							reviewNote = r.ReviewNote ?? "",
							createdAt = r.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "",
							updatedAt = r.UpdatedAt?.ToString("dd/MM/yyyy HH:mm") ?? ""
						}
					});
				}

				if (type == "Leave")
				{
					var r = await _context.LeaveRequests
						.Include(x => x.User)
						.FirstOrDefaultAsync(x => x.LeaveRequestId == id);

					if (r == null)
						return Json(new { success = false, message = "Không tìm thấy request" });

					return Json(new
					{
						success = true,
						request = new
						{
							leaveRequestId = r.LeaveRequestId,
							userId = r.UserId,
							employeeName = r.User?.FullName ?? "N/A",
							leaveType = r.LeaveType ?? "",
							startDate = r.StartDate.ToString("dd/MM/yyyy"),
							endDate = r.EndDate.ToString("dd/MM/yyyy"),
							totalDays = r.TotalDays,
							reason = r.Reason ?? "",
							proofDocument = r.ProofDocument ?? "",
							status = r.Status ?? "Pending",
							reviewedBy = r.ReviewedBy ?? 0,
							reviewedAt = r.ReviewedAt?.ToString("dd/MM/yyyy HH:mm") ?? "",
							reviewNote = r.ReviewNote ?? "",
							createdAt = r.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "",
							updatedAt = r.UpdatedAt?.ToString("dd/MM/yyyy HH:mm") ?? ""
						}
					});
				}

				if (type == "Late")
				{
					var r = await _context.LateRequests
						.Include(x => x.User)
						.FirstOrDefaultAsync(x => x.LateRequestId == id);

					if (r == null)
						return Json(new { success = false, message = "Không tìm thấy request" });

					return Json(new
					{
						success = true,
						request = new
						{
							lateRequestId = r.LateRequestId,
							userId = r.UserId,
							employeeName = r.User?.FullName ?? "N/A",
							requestDate = r.RequestDate.ToString("dd/MM/yyyy"),
							expectedArrivalTime = r.ExpectedArrivalTime.ToString("HH:mm"),
							reason = r.Reason ?? "",
							proofDocument = r.ProofDocument ?? "",
							status = r.Status ?? "Pending",
							reviewedBy = r.ReviewedBy ?? 0,
							reviewedAt = r.ReviewedAt?.ToString("dd/MM/yyyy HH:mm") ?? "",
							reviewNote = r.ReviewNote ?? "",
							createdAt = r.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "",
							updatedAt = r.UpdatedAt?.ToString("dd/MM/yyyy HH:mm") ?? ""
						}
					});
				}

				return Json(new { success = false, message = "Loại request không hợp lệ" });
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"GetRequestDetail Error: {ex.Message}\n{ex.StackTrace}");
				return Json(new { success = false, message = $"Lỗi server: {ex.Message}" });
			}
		}

		[HttpGet]
		public async Task<IActionResult> PendingRequests(string? type, string? status, DateTime? fromDate, DateTime? toDate, string? keyword)
		{
			if (!IsAdmin()) return RedirectToAction("Login", "Account");

			var from = fromDate ?? DateTime.Today.AddMonths(-1);
			var to = toDate ?? DateTime.Today.AddDays(1);

			var vm = new TMDSystem.Models.ViewModels.PendingRequestsViewModel
			{
				SelectedType = type
			};

			// Prepare base queries
			IQueryable<OvertimeRequest> otQuery = _context.OvertimeRequests.Include(r => r.User).AsQueryable();
			IQueryable<LeaveRequest> leaveQuery = _context.LeaveRequests.Include(r => r.User).AsQueryable();
			IQueryable<LateRequest> lateQuery = _context.LateRequests.Include(r => r.User).AsQueryable();

			// Date range filter (CreatedAt)
			otQuery = otQuery.Where(r => r.CreatedAt >= from && r.CreatedAt <= to);
			leaveQuery = leaveQuery.Where(r => r.CreatedAt >= from && r.CreatedAt <= to);
			lateQuery = lateQuery.Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

			// Status filter (if provided)
			if (!string.IsNullOrWhiteSpace(status))
			{
				otQuery = otQuery.Where(r => r.Status == status);
				leaveQuery = leaveQuery.Where(r => r.Status == status);
				lateQuery = lateQuery.Where(r => r.Status == status);
			}

			// Keyword filter (if provided) - search in reason, task description, proof document
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				var kw = keyword.Trim().ToLower();
				otQuery = otQuery.Where(r => (r.Reason ?? "").ToLower().Contains(kw) || (r.TaskDescription ?? "").ToLower().Contains(kw));
				leaveQuery = leaveQuery.Where(r => (r.Reason ?? "").ToLower().Contains(kw) || (r.ProofDocument ?? "").ToLower().Contains(kw));
				lateQuery = lateQuery.Where(r => (r.Reason ?? "").ToLower().Contains(kw) || (r.ProofDocument ?? "").ToLower().Contains(kw));
			}

			// Only fetch types requested (to save queries)
			if (string.IsNullOrEmpty(type) || type == "Overtime")
			{
				vm.Overtime = await otQuery
					.OrderByDescending(r => r.CreatedAt)
					.ToListAsync();
			}

			if (string.IsNullOrEmpty(type) || type == "Leave")
			{
				vm.Leave = await leaveQuery
					.OrderByDescending(r => r.CreatedAt)
					.ToListAsync();
			}

			if (string.IsNullOrEmpty(type) || type == "Late")
			{
				vm.Late = await lateQuery
					.OrderByDescending(r => r.CreatedAt)
					.ToListAsync();
			}

			// Preserve view state for UI
			ViewBag.Type = type;
			ViewBag.FilterStatus = status;
			ViewBag.FromDate = fromDate;
			ViewBag.ToDate = toDate;
			ViewBag.Keyword = keyword;

			// Calculate statistics for display
			var allOt = vm.Overtime ?? new List<OvertimeRequest>();
			var allLeave = vm.Leave ?? new List<LeaveRequest>();
			var allLate = vm.Late ?? new List<LateRequest>();

			ViewBag.TotalPending = allOt.Count(r => r.Status == "Pending") +
								   allLeave.Count(r => r.Status == "Pending") +
								   allLate.Count(r => r.Status == "Pending");

			ViewBag.TotalApproved = allOt.Count(r => r.Status == "Approved") +
									allLeave.Count(r => r.Status == "Approved") +
									allLate.Count(r => r.Status == "Approved");

			ViewBag.TotalRejected = allOt.Count(r => r.Status == "Rejected") +
									allLeave.Count(r => r.Status == "Rejected") +
									allLate.Count(r => r.Status == "Rejected");

			return View(vm);
		}


		[HttpPost]
		public async Task<IActionResult> ReviewRequest([FromBody] ReviewRequestViewModel model)
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Không có quyền" });

			var adminId = HttpContext.Session.GetInt32("UserId");
			try
			{
				if (model.RequestType == "Overtime")
				{
					var r = await _context.OvertimeRequests.FindAsync(model.RequestId);
					if (r == null) return Json(new { success = false, message = "Không tìm thấy" });

					var old = new { r.Status, r.ReviewedBy, r.ReviewedAt, r.ReviewNote };
					if (model.Action == "Approve")
					{
						r.Status = "Approved";
						r.ReviewedBy = adminId;
						r.ReviewedAt = DateTime.Now;
						r.ReviewNote = model.Note;
						// update attendance approved overtime
						var att = await _context.Attendances.FirstOrDefaultAsync(a => a.UserId == r.UserId && a.WorkDate == r.WorkDate);
						if (att != null)
						{
							att.IsOvertimeApproved = true;
							att.ApprovedOvertimeHours = r.OvertimeHours;
							await _context.SaveChangesAsync();
						}
					}
					else if (model.Action == "Reject")
					{
						r.Status = "Rejected";
						r.ReviewedBy = adminId;
						r.ReviewedAt = DateTime.Now;
						r.ReviewNote = model.Note;
					}

					r.UpdatedAt = DateTime.Now;
					await _context.SaveChangesAsync();

					await _auditHelper.LogDetailedAsync(adminId, "REVIEW", "OvertimeRequest", r.OvertimeRequestId, old, r, $"Admin {model.Action} overtime request #{r.OvertimeRequestId}", new Dictionary<string, object> { { "Note", model.Note } });
					return Json(new { success = true, message = "Đã xử lý request" });
				}

				if (model.RequestType == "Leave")
				{
					var r = await _context.LeaveRequests.FindAsync(model.RequestId);
					if (r == null) return Json(new { success = false, message = "Không tìm thấy" });
					var old = new { r.Status, r.ReviewedBy, r.ReviewedAt, r.ReviewNote };

					if (model.Action == "Approve")
					{
						r.Status = "Approved";
						r.ReviewedBy = adminId;
						r.ReviewedAt = DateTime.Now;
						r.ReviewNote = model.Note;

						// optionally apply to attendance: mark days as approved leave (business logic)
						// not forcing DB changes here; admin can adjust attendance if required.
					}
					else if (model.Action == "Reject")
					{
						r.Status = "Rejected";
						r.ReviewedBy = adminId;
						r.ReviewedAt = DateTime.Now;
						r.ReviewNote = model.Note;
					}

					r.UpdatedAt = DateTime.Now;
					await _context.SaveChangesAsync();

					await _auditHelper.LogDetailedAsync(adminId, "REVIEW", "LeaveRequest", r.LeaveRequestId, old, r, $"Admin {model.Action} leave request #{r.LeaveRequestId}", new Dictionary<string, object> { { "Note", model.Note } });
					return Json(new { success = true, message = "Đã xử lý request" });
				}

				if (model.RequestType == "Late")
				{
					var r = await _context.LateRequests.FindAsync(model.RequestId);
					if (r == null) return Json(new { success = false, message = "Không tìm thấy" });
					var old = new { r.Status, r.ReviewedBy, r.ReviewedAt, r.ReviewNote };

					if (model.Action == "Approve")
					{
						r.Status = "Approved";
						r.ReviewedBy = adminId;
						r.ReviewedAt = DateTime.Now;
						r.ReviewNote = model.Note;

						// update attendance if exists: clear late flag or mark reviewed
						var att = await _context.Attendances.FirstOrDefaultAsync(a => a.UserId == r.UserId && a.WorkDate == r.RequestDate);
						if (att != null)
						{
							att.HasLateRequest = true;
							att.LateRequestId = r.LateRequestId;
							// possibly set DeductionHours = 0 or adjust
							await _context.SaveChangesAsync();
						}
					}
					else if (model.Action == "Reject")
					{
						r.Status = "Rejected";
						r.ReviewedBy = adminId;
						r.ReviewedAt = DateTime.Now;
						r.ReviewNote = model.Note;
					}

					r.UpdatedAt = DateTime.Now;
					await _context.SaveChangesAsync();

					await _auditHelper.LogDetailedAsync(adminId, "REVIEW", "LateRequest", r.LateRequestId, old, r, $"Admin {model.Action} late request #{r.LateRequestId}", new Dictionary<string, object> { { "Note", model.Note } });
					return Json(new { success = true, message = "Đã xử lý request" });
				}

				return Json(new { success = false, message = "Loại request không hợp lệ" });
			}
			catch (Exception ex)
			{
				await _auditHelper.LogFailedAttemptAsync(adminId, "REVIEW", "Request", $"Exception: {ex.Message}", new { Error = ex.ToString(), model });
				return Json(new { success = false, message = $"Có lỗi: {ex.Message}" });
			}
		}


		// ============================================
		// REQUEST MODELS
		// ============================================
		public class CreateDepartmentRequest
		{
			public string DepartmentName { get; set; } = string.Empty;
			public string? Description { get; set; }
			public bool IsActive { get; set; } = true;
		}

		public class UpdateDepartmentRequest
		{
			public int DepartmentId { get; set; }
			public string DepartmentName { get; set; } = string.Empty;
			public string? Description { get; set; }
			public bool IsActive { get; set; }
		}

		public class DeleteDepartmentRequest
		{
			public int DepartmentId { get; set; }
		}

		public class ToggleDepartmentRequest
		{
			public int DepartmentId { get; set; }
		}

		public class ResetPasswordRequest
		{
			public int UserId { get; set; }
			public string NewPassword { get; set; } = string.Empty;
			public string Reason { get; set; } = string.Empty;
		}

		public class ToggleUserRequest
		{
			public int UserId { get; set; }
		}

		public class CreateTaskRequest
		{
			public string TaskName { get; set; } = string.Empty;
			public string? Description { get; set; }
			public string? Platform { get; set; }
			public int TargetPerWeek { get; set; }
			public DateTime? Deadline { get; set; }
			public string? Priority { get; set; }
			public List<int>? AssignedUserIds { get; set; }
		}

		public class UpdateTaskRequest
		{
			public int TaskId { get; set; }
			public string TaskName { get; set; } = string.Empty;
			public string? Description { get; set; }
			public string? Platform { get; set; }
			public int TargetPerWeek { get; set; }
			public DateTime? Deadline { get; set; }
			public string? Priority { get; set; }
			public List<int>? AssignedUserIds { get; set; }
		}

		public class DeleteTaskRequest
		{
			public int TaskId { get; set; }
		}

		public class ToggleTaskStatusRequest
		{
			public int TaskId { get; set; }
		}
	}
}
