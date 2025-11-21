using System.Collections.Generic;
using TMD.Models;

namespace TMDSystem.Models.ViewModels
{
	public class PendingRequestsViewModel
	{
		public List<OvertimeRequest> Overtime { get; set; } = new();
		public List<LeaveRequest> Leave { get; set; } = new();
		public List<LateRequest> Late { get; set; } = new();
		public string? SelectedType { get; set; }
	}
}
