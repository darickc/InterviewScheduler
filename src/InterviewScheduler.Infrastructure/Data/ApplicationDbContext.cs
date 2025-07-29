using Microsoft.EntityFrameworkCore;
using InterviewScheduler.Core.Entities;

namespace InterviewScheduler.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Leader> Leaders { get; set; }
    public DbSet<AppointmentType> AppointmentTypes { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Contact entity configuration
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20);
            
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
        });
        
        // Seed data for appointment types
        modelBuilder.Entity<AppointmentType>().HasData(
            new AppointmentType
            {
                Id = 1,
                Name = "Generic Meeting",
                Duration = 15,
                MessageTemplate = "{ContactName}, can you meet with {LeaderName} on {Date} at {Time}?",
                MinorMessageTemplate = "Dear Parent/Guardian of {ContactName}, your child has been scheduled to meet with {LeaderName} on {Date} at {Time}. Please ensure they are available."
            },
            new AppointmentType
            {
                Id = 2,
                Name = "Temple Recommend Interview",
                Duration = 10,
                MessageTemplate = "{ContactName}, your temple recommend has expired or is about to expire. Can you meet with {LeaderName} for a temple recommend interview on {Date} at {Time}?",
                MinorMessageTemplate = "Dear Parent/Guardian of {ContactName}, your child's temple recommend requires renewal. They have been scheduled with {LeaderName} on {Date} at {Time} for their interview."
            }
        );
    }
}