using Microsoft.EntityFrameworkCore;
using InterviewScheduler.Core.Entities;

namespace InterviewScheduler.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Leader> Leaders { get; set; }
    public DbSet<AppointmentType> AppointmentTypes { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.GoogleUserId)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.HasIndex(e => e.GoogleUserId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
        
        // Contact entity configuration
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.MiddleName)
                .HasMaxLength(100);
                
            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20);
            
            // User relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.Contacts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Self-referencing relationships
            entity.HasOne(e => e.HeadOfHouse)
                .WithMany(e => e.HouseholdMembers)
                .HasForeignKey(e => e.HeadOfHouseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Spouse)
                .WithOne()
                .HasForeignKey<Contact>(e => e.SpouseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.Ignore(e => e.FullName)
                .Ignore(e => e.DisplayName)
                .Ignore(e => e.Age)
                .Ignore(e => e.IsMinor)
                .Ignore(e => e.Salutation);
        });
        
        // Leader entity configuration
        modelBuilder.Entity<Leader>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.GoogleCalendarId)
                .IsRequired()
                .HasMaxLength(255);
                
            // User relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.Leaders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // AppointmentType entity configuration
        modelBuilder.Entity<AppointmentType>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.MessageTemplate)
                .IsRequired()
                .HasMaxLength(1000);
                
            entity.Property(e => e.MinorMessageTemplate)
                .IsRequired()
                .HasMaxLength(1000);
                
            // User relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.AppointmentTypes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Appointment entity configuration
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.GoogleEventId)
                .HasMaxLength(255);
            
            entity.HasOne(e => e.Contact)
                .WithMany(e => e.Appointments)
                .HasForeignKey(e => e.ContactId);
                
            entity.HasOne(e => e.Leader)
                .WithMany(e => e.Appointments)
                .HasForeignKey(e => e.LeaderId);
                
            entity.HasOne(e => e.AppointmentType)
                .WithMany(e => e.Appointments)
                .HasForeignKey(e => e.AppointmentTypeId);
                
            // User relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.Appointments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}