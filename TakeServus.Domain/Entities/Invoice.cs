namespace TakeServus.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }

    public Job Job { get; set; } = default!;
}