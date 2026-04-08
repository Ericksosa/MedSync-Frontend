namespace MedSync_Frontend.Models.ViewModels;

public class NotificacionItemViewModel
{
    public string Id { get; set; } = "";
    public string Tipo { get; set; } = "info";
    public string Titulo { get; set; } = "";
    public string Mensaje { get; set; } = "";
    public string? Link { get; set; }
    public DateTime? Fecha { get; set; }
    public bool Leida { get; set; }
}

public class NotificacionFeedViewModel
{
    public int NoLeidas { get; set; }
    public List<NotificacionItemViewModel> Items { get; set; } = [];
}

public class MarcarNotificacionRequest
{
    public string Id { get; set; } = "";
}
