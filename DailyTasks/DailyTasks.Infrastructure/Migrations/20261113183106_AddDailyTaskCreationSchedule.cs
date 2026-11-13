using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyTasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyTaskCreationSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationWindowEndsOn",
                schema: "dailytasks",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "UserDueDateWindowDays",
                schema: "dailytasks",
                table: "Settings");

            migrationBuilder.AddColumn<bool>(
                name: "AllowWeekendCreation",
                schema: "dailytasks",
                table: "Settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CreationEndTime",
                schema: "dailytasks",
                table: "Settings",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(19, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CreationStartTime",
                schema: "dailytasks",
                table: "Settings",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(8, 30, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowWeekendCreation",
                schema: "dailytasks",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CreationEndTime",
                schema: "dailytasks",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CreationStartTime",
                schema: "dailytasks",
                table: "Settings");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CreationWindowEndsOn",
                schema: "dailytasks",
                table: "Settings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserDueDateWindowDays",
                schema: "dailytasks",
                table: "Settings",
                type: "integer",
                nullable: false,
                defaultValue: 7);
        }
    }
}
