using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto.Migrations
{
    /// <inheritdoc />
    public partial class AutoregistroSinCodigoDeEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Equipos_CodigoRegistro",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "CodigoRegistro",
                table: "Equipos");

            migrationBuilder.AlterColumn<long>(
                name: "EquipoId",
                table: "Usuarios",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "EquipoId",
                table: "Usuarios",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoRegistro",
                table: "Equipos",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_CodigoRegistro",
                table: "Equipos",
                column: "CodigoRegistro",
                unique: true);
        }
    }
}
