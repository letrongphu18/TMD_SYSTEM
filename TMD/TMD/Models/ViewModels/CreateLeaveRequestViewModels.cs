public class CreateLeaveRequestViewModel
{
	public int UserId { get; set; }
	public DateTime StartDate { get; set; }
	public DateTime EndDate { get; set; }
	public decimal TotalDays { get; set; }
	public string LeaveType { get; set; } = "Annual";
	public string Reason { get; set; } = string.Empty;
	public string? ProofDocument { get; set; } // optional path or uploaded filename
}
