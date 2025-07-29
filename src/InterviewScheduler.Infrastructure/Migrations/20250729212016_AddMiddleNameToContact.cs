using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMiddleNameToContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Contacts",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Contacts");
        }
    }
}
