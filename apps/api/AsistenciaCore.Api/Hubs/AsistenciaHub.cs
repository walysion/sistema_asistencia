using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AsistenciaCore.Api.Hubs;

[Authorize]
public class AsistenciaHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var empresaIdClaim = Context.User?.FindFirst("EmpresaId")?.Value;
        if (!string.IsNullOrEmpty(empresaIdClaim))
        {
            // Suscribir automáticamente la conexión al grupo aislado de su Empresa
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Empresa_{empresaIdClaim}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var empresaIdClaim = Context.User?.FindFirst("EmpresaId")?.Value;
        if (!string.IsNullOrEmpty(empresaIdClaim))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Empresa_{empresaIdClaim}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}