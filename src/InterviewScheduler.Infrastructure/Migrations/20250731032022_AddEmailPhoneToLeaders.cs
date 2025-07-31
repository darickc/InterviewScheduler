using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailPhoneToLeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Leaders",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Leaders",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Leaders");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Leaders");
        }
    }
}
