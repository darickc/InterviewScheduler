using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnableUnrestrictedSchedulingForAppointmentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all existing AppointmentType records to allow unrestricted scheduling
            migrationBuilder.Sql(@"
                UPDATE AppointmentTypes 
                SET AllowWeekendScheduling = 1, 
                    AllowAfterHoursScheduling = 1 
                WHERE AllowWeekendScheduling = 0 OR AllowAfterHoursScheduling = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to restricted scheduling (this may not be desired in practice)
            migrationBuilder.Sql(@"
                UPDATE AppointmentTypes 
                SET AllowWeekendScheduling = 0, 
                    AllowAfterHoursScheduling = 0;
            ");
        }
    }
}
