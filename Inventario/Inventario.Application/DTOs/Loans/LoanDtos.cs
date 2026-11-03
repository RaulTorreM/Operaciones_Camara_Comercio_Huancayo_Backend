using Inventario.Core.Enums;

namespace Inventario.Application.DTOs.Loans;

public record LoanItemDto(int Id, int AssetId, string Name, string? Code, AssetCondition Condition, bool Returned, AssetCondition? ReturnCondition);

public record LoanDto(
    int Id, string Borrower, DateTime OutAt, DateOnly DueDate, DateTime? ReturnedAt, string? Notes,
    string User, bool IsOverdue, List<LoanItemDto> Items);

public record CreateLoanRequest(string? Borrower, DateOnly DueDate, List<int>? AssetIds, string? Notes);
public record ReturnItemRequest(int AssetId, AssetCondition Condition);
public record ReturnLoanRequest(List<ReturnItemRequest>? Items);
public record ReturnLoanResultDto(bool Complete);
