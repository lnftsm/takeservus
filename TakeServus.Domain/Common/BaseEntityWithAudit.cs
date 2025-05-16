namespace TakeServus.Domain.Common;

public abstract class BaseEntityWithAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public bool IsDeleted { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserFullName { get; set; }

    public DateTime? ModifiedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedByUserId { get; set; }
    public string? ModifiedByUserFullName { get; set; }
}