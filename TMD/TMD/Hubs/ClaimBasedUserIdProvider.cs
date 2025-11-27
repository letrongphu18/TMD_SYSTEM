using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

public class ClaimBasedUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? connection.User?.FindFirst("sub")?.Value;
    }
}