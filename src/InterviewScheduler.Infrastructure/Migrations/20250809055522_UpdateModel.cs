using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowAfterHoursScheduling",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowWeekendScheduling",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BufferTimeAfterMinutes",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BufferTimeBeforeMinutes",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ColorCode",
                table: "AppointmentTypes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaximumAdvanceBookingDays",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaximumDurationMinutes",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinimumAdvanceBookingHours",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinimumDurationMinutes",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireStrictBufferTime",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SchedulingPriority",
                table: "AppointmentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowAfterHoursScheduling",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "AllowWeekendScheduling",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "BufferTimeAfterMinutes",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "BufferTimeBeforeMinutes",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "ColorCode",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "MaximumAdvanceBookingDays",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "MaximumDurationMinutes",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "MinimumAdvanceBookingHours",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "MinimumDurationMinutes",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "RequireStrictBufferTime",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "SchedulingPriority",
                table: "AppointmentTypes");
        }
    }
}
