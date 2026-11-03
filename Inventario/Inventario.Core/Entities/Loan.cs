namespace Inventario.Core.Entities;

/// <summary>Préstamo o alquiler de materiales a un asociado para un evento.</summary>
public class Loan
{
    public int Id { get; set; }
    public string Borrower { get; set; } = "";
    public DateTime OutAt { get; set; } = DateTime.UtcNow;
    public DateOnly DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? Notes { get; set; }
    public string User { get; set; } = "";
    public List<LoanItem> Items { get; set; } = new();
}
