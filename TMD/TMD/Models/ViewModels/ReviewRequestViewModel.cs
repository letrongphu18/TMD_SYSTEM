public class ReviewRequestViewModel
{
	public int RequestId { get; set; }
	public string RequestType { get; set; } = string.Empty; // "Overtime","Leave","Late"
	public string Action { get; set; } = string.Empty; // "Approve","Reject","Cancel"
	public string? Note { get; set; }
}
