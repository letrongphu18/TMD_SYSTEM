public class CreateOvertimeRequestViewModel
{
	public int UserId { get; set; } // server sẽ lấy từ session; client không cần gửi
	public DateTime WorkDate { get; set; }
	public DateTime ActualCheckOutTime { get; set; }
	public decimal OvertimeHours { get; set; }
	public string Reason { get; set; } = string.Empty;
	public string? TaskDescription { get; set; }
}
