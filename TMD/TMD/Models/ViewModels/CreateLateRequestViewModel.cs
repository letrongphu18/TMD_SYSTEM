public class CreateLateRequestViewModel
{
	public int UserId { get; set; }
	public DateTime RequestDate { get; set; } // day they were late
	public TimeSpan ExpectedArrivalTime { get; set; }
	public string Reason { get; set; } = string.Empty;
	public string? ProofDocument { get; set; }
}
