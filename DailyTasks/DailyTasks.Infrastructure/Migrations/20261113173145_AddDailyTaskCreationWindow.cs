using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyTasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyTaskCreationWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CreationWindowEndsOn",
                schema: "dailytasks",
                table: "Settings",
                type: "date",
                nullable: true);

            // La configuración existente se convierte en una ventana activa desde hoy,
            // usando los días ya guardados (7 por defecto).
            migrationBuilder.Sql(
                "UPDATE dailytasks.\"Settings\" " +
                "SET \"CreationWindowEndsOn\" = CURRENT_DATE + \"UserDueDateWindowDays\" " +
                "WHERE \"CreationWindowEndsOn\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationWindowEndsOn",
                schema: "dailytasks",
                table: "Settings");
        }
    }
}
