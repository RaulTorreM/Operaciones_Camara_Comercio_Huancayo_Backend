using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventario.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase3Fase4Integridad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_RoomId",
                schema: "inventory",
                table: "Assets");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "inventory",
                table: "Supplies",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "inventory",
                table: "Loans",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "inventory",
                table: "LoanItems",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "inventory",
                table: "Assets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ReturnedAt_DueDate",
                schema: "inventory",
                table: "Loans",
                columns: new[] { "ReturnedAt", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_RoomId_Verification",
                schema: "inventory",
                table: "Assets",
                columns: new[] { "RoomId", "Verification" });

            // Unicidad sin distinguir mayúsculas ni espacios exteriores.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Assets_Code_Lower"
                    ON inventory."Assets" (lower(btrim("Code")))
                    WHERE "Code" IS NOT NULL;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Rooms_Name_Lower"
                    ON inventory."Rooms" (lower(btrim("Name")));
                """);

            // Un bien no puede estar en dos préstamos activos a la vez.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_LoanItems_AssetId_Active"
                    ON inventory."LoanItems" ("AssetId")
                    WHERE "ReturnedAt" IS NULL;
                """);

            // Reglas de dominio a nivel de base de datos (defensa adicional a las validaciones del servicio).
            migrationBuilder.Sql("""
                ALTER TABLE inventory."Assets"
                    ADD CONSTRAINT "CK_Assets_Quantity"
                    CHECK (("Type" = 'Individual' AND "Quantity" = 1) OR ("Type" = 'Lot' AND "Quantity" >= 0));

                ALTER TABLE inventory."Assets"
                    ADD CONSTRAINT "CK_Assets_Type"
                    CHECK ("Type" IN ('Individual', 'Lot'));

                ALTER TABLE inventory."Assets"
                    ADD CONSTRAINT "CK_Assets_Condition"
                    CHECK ("Condition" IN ('Good', 'Fair', 'Poor', 'Decommissioned'));

                ALTER TABLE inventory."Assets"
                    ADD CONSTRAINT "CK_Assets_Verification"
                    CHECK ("Verification" IN ('Pending', 'Verified', 'NotFound'));

                ALTER TABLE inventory."Assets"
                    ADD CONSTRAINT "CK_Assets_Name_NotEmpty"
                    CHECK (btrim("Name") <> '');

                ALTER TABLE inventory."Rooms"
                    ADD CONSTRAINT "CK_Rooms_Name_NotEmpty"
                    CHECK (btrim("Name") <> '');

                ALTER TABLE inventory."Supplies"
                    ADD CONSTRAINT "CK_Supplies_Name_NotEmpty"
                    CHECK (btrim("Name") <> '');

                ALTER TABLE inventory."Supplies"
                    ADD CONSTRAINT "CK_Supplies_Stock"
                    CHECK (("Mode" = 'Quantity' AND "Stock" >= 0 AND "MinStock" >= 0 AND "Level" = 3)
                        OR ("Mode" = 'Visual' AND "Stock" = 0 AND "MinStock" = 0 AND "Level" BETWEEN 0 AND 3));

                ALTER TABLE inventory."SupplyMovements"
                    ADD CONSTRAINT "CK_SupplyMovements_Type"
                    CHECK ("Type" IN ('In', 'Out', 'Adjustment'));

                ALTER TABLE inventory."LoanItems"
                    ADD CONSTRAINT "CK_LoanItems_ReturnCondition"
                    CHECK ("ReturnCondition" IS NULL OR "ReturnCondition" IN ('Good', 'Fair', 'Poor', 'Decommissioned'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE inventory."Assets" DROP CONSTRAINT IF EXISTS "CK_Assets_Quantity";
                ALTER TABLE inventory."Assets" DROP CONSTRAINT IF EXISTS "CK_Assets_Type";
                ALTER TABLE inventory."Assets" DROP CONSTRAINT IF EXISTS "CK_Assets_Condition";
                ALTER TABLE inventory."Assets" DROP CONSTRAINT IF EXISTS "CK_Assets_Verification";
                ALTER TABLE inventory."Assets" DROP CONSTRAINT IF EXISTS "CK_Assets_Name_NotEmpty";
                ALTER TABLE inventory."Rooms" DROP CONSTRAINT IF EXISTS "CK_Rooms_Name_NotEmpty";
                ALTER TABLE inventory."Supplies" DROP CONSTRAINT IF EXISTS "CK_Supplies_Name_NotEmpty";
                ALTER TABLE inventory."Supplies" DROP CONSTRAINT IF EXISTS "CK_Supplies_Stock";
                ALTER TABLE inventory."SupplyMovements" DROP CONSTRAINT IF EXISTS "CK_SupplyMovements_Type";
                ALTER TABLE inventory."LoanItems" DROP CONSTRAINT IF EXISTS "CK_LoanItems_ReturnCondition";

                DROP INDEX IF EXISTS inventory."IX_LoanItems_AssetId_Active";
                DROP INDEX IF EXISTS inventory."IX_Assets_Code_Lower";
                DROP INDEX IF EXISTS inventory."IX_Rooms_Name_Lower";
                """);

            migrationBuilder.DropIndex(
                name: "IX_Loans_ReturnedAt_DueDate",
                schema: "inventory",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Assets_RoomId_Verification",
                schema: "inventory",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "inventory",
                table: "Supplies");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "inventory",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "inventory",
                table: "LoanItems");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "inventory",
                table: "Assets");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_RoomId",
                schema: "inventory",
                table: "Assets",
                column: "RoomId");
        }
    }
}
