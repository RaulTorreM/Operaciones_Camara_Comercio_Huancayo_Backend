namespace Inventario.Application.DTOs.Summary;

public record SummaryDto(int Assets, int Verified, int NotFound, int Pending, int LowSupplies, int ActiveLoans, int OverdueLoans);
