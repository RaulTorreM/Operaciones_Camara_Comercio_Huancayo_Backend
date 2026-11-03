using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DailyTasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDailyTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dailytasks");

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dailytasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Area = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                schema: "dailytasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Area = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: false),
                    AssignedToId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedById = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_AssignedToId",
                        column: x => x.AssignedToId,
                        principalSchema: "dailytasks",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "dailytasks",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalSchema: "dailytasks",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskHistory",
                schema: "dailytasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskId = table.Column<int>(type: "integer", nullable: false),
                    ChangedById = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangeGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Field = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskHistory_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "dailytasks",
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskHistory_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalSchema: "dailytasks",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskHistory_ChangedById",
                schema: "dailytasks",
                table: "TaskHistory",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_TaskHistory_TaskId_ChangedAt",
                schema: "dailytasks",
                table: "TaskHistory",
                columns: new[] { "TaskId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Area_Status",
                schema: "dailytasks",
                table: "Tasks",
                columns: new[] { "Area", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedToId_Status",
                schema: "dailytasks",
                table: "Tasks",
                columns: new[] { "AssignedToId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CompletedOn",
                schema: "dailytasks",
                table: "Tasks",
                column: "CompletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedById",
                schema: "dailytasks",
                table: "Tasks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedOn",
                schema: "dailytasks",
                table: "Tasks",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DeletedAt_Status_DueDate",
                schema: "dailytasks",
                table: "Tasks",
                columns: new[] { "DeletedAt", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DeletedById",
                schema: "dailytasks",
                table: "Tasks",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DueDate",
                schema: "dailytasks",
                table: "Tasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Priority",
                schema: "dailytasks",
                table: "Tasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Area",
                schema: "dailytasks",
                table: "Users",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                schema: "dailytasks",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskHistory",
                schema: "dailytasks");

            migrationBuilder.DropTable(
                name: "Tasks",
                schema: "dailytasks");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dailytasks");
        }
    }
}
