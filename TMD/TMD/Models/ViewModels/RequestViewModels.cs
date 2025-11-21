// ✅ KHÔNG CẦN SỬA GÌ - ViewModels của bạn đã đúng rồi!

using System.ComponentModel.DataAnnotations;

namespace TMDSystem.Models.ViewModels
{
	// ===== OVERTIME REQUEST =====
	public class CreateOvertimeRequestViewModel
	{
		[Required(ErrorMessage = "Vui lòng chọn ngày làm việc")]
		public DateTime WorkDate { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn giờ kết thúc")]
		[RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Định dạng giờ không hợp lệ (HH:mm)")]
		public string ActualCheckOutTime { get; set; } = null!;  // ✅ ĐÚNG - giữ nguyên STRING

		[Required(ErrorMessage = "Vui lòng nhập số giờ tăng ca")]
		[Range(0.1, 12, ErrorMessage = "Số giờ tăng ca phải từ 0.1 đến 12")]
		public decimal OvertimeHours { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập lý do")]
		[StringLength(1000, ErrorMessage = "Lý do không được quá 1000 ký tự")]
		public string Reason { get; set; } = null!;

		[StringLength(1000, ErrorMessage = "Mô tả công việc không được quá 1000 ký tự")]
		public string? TaskDescription { get; set; }
	}

	// ===== LEAVE REQUEST =====
	public class CreateLeaveRequestViewModel
	{
		[Required(ErrorMessage = "Vui lòng chọn loại nghỉ phép")]
		public string LeaveType { get; set; } = null!;

		[Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
		public DateTime StartDate { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
		public DateTime EndDate { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập số ngày")]
		[Range(0.5, 365, ErrorMessage = "Số ngày phải từ 0.5 đến 365")]
		public decimal TotalDays { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập lý do")]
		[StringLength(1000, ErrorMessage = "Lý do không được quá 1000 ký tự")]
		public string Reason { get; set; } = null!;

		[Url(ErrorMessage = "URL không hợp lệ")]
		[StringLength(500, ErrorMessage = "URL không được quá 500 ký tự")]
		public string? ProofDocument { get; set; }
	}

	// ===== LATE REQUEST =====
	public class CreateLateRequestViewModel
	{
		[Required(ErrorMessage = "Vui lòng chọn ngày")]
		public DateTime RequestDate { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn giờ đến dự kiến")]
		public TimeSpan ExpectedArrivalTime { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập lý do")]
		[StringLength(1000, ErrorMessage = "Lý do không được quá 1000 ký tự")]
		public string Reason { get; set; } = null!;

		[Url(ErrorMessage = "URL không hợp lệ")]
		[StringLength(500, ErrorMessage = "URL không được quá 500 ký tự")]
		public string? ProofDocument { get; set; }
	}

	// ===== REVIEW REQUEST =====
	public class ReviewRequestViewModel
	{
		[Required]
		public int RequestId { get; set; }

		[Required]
		public string RequestType { get; set; } = null!;

		public string? ReviewNote { get; set; }
	}
}