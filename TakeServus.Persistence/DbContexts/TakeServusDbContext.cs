using Microsoft.EntityFrameworkCore;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.Configurations;

namespace TakeServus.Persistence.DbContexts;

public class TakeServusDbContext : DbContext
{
    public TakeServusDbContext(DbContextOptions<TakeServusDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<JobMaterial> JobMaterials => Set<JobMaterial>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<JobNote> JobNotes => Set<JobNote>();
    public DbSet<JobPhoto> JobPhotos => Set<JobPhoto>();
    public DbSet<JobActivity> JobActivities => Set<JobActivity>();
    public DbSet<JobFeedback> JobFeedbacks => Set<JobFeedback>();

    public DbSet<QueuedEmail> QueuedEmails => Set<QueuedEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new TechnicianConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new JobConfiguration());
        modelBuilder.ApplyConfiguration(new MaterialConfiguration());
        modelBuilder.ApplyConfiguration(new JobMaterialConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new JobNoteConfiguration());
        modelBuilder.ApplyConfiguration(new JobPhotoConfiguration());
        modelBuilder.ApplyConfiguration(new JobActivityConfiguration());
        modelBuilder.ApplyConfiguration(new JobFeedbackConfiguration());
        modelBuilder.ApplyConfiguration(new QueuedEmailConfiguration());
    }
}
