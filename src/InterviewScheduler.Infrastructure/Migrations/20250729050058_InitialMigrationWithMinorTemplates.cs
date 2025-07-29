using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InterviewScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrationWithMinorTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageTemplate = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    MinorMessageTemplate = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HeadOfHouseId = table.Column<int>(type: "INTEGER", nullable: true),
                    SpouseId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_Contacts_HeadOfHouseId",
                        column: x => x.HeadOfHouseId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contacts_Contacts_SpouseId",
                        column: x => x.SpouseId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Leaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GoogleCalendarId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContactId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaderId = table.Column<int>(type: "INTEGER", nullable: false),
                    AppointmentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GoogleEventId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_AppointmentTypes_AppointmentTypeId",
                        column: x => x.AppointmentTypeId,
                        principalTable: "AppointmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Leaders_LeaderId",
                        column: x => x.LeaderId,
                        principalTable: "Leaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppointmentTypes",
                columns: new[] { "Id", "Duration", "MessageTemplate", "MinorMessageTemplate", "Name" },
                values: new object[,]
                {
                    { 1, 15, "{ContactName}, can you meet with {LeaderName} on {Date} at {Time}?", "Dear Parent/Guardian of {ContactName}, your child has been scheduled to meet with {LeaderName} on {Date} at {Time}. Please ensure they are available.", "Generic Meeting" },
                    { 2, 10, "{ContactName}, your temple recommend has expired or is about to expire. Can you meet with {LeaderName} for a temple recommend interview on {Date} at {Time}?", "Dear Parent/Guardian of {ContactName}, your child's temple recommend requires renewal. They have been scheduled with {LeaderName} on {Date} at {Time} for their interview.", "Temple Recommend Interview" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentTypeId",
                table: "Appointments",
                column: "AppointmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ContactId",
                table: "Appointments",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_LeaderId",
                table: "Appointments",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_HeadOfHouseId",
                table: "Contacts",
                column: "HeadOfHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_SpouseId",
                table: "Contacts",
                column: "SpouseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "AppointmentTypes");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "Leaders");
        }
    }
}
