namespace front.Models;

public class CreateReportDto
{
    public string UserId { get; set; } = default!;   // wymagany
    public string Title { get; set; } = "";          // krótki tytuł
    public string? Description { get; set; }         // opis
    public double Lat { get; set; }                  // współrzędne zdarzenia
    public double Lon { get; set; }
    public string? RouteId { get; set; }             // opcjonalnie: linia/route
    public string? TripId { get; set; }              // opcjonalnie: kurs/trip
    public int? DelayMinutes { get; set; }           // opcjonalnie: opóźnienie
}

public sealed class ReportDto : CreateReportDto
{
    public string Id { get; set; } = default!;       // Id zgłoszenia
    public long CreatedAt { get; set; }              // epoch (s)
    public string Status { get; set; } = "pending";  // pending/verified/rejected/expired
    public int Score { get; set; }                   // na przyszłość (głosy)
}