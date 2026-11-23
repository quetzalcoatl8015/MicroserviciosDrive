using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto.Migrations
{
    /// <inheritdoc />
    public partial class SelloDeSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelloSesion",
                table: "Usuarios",
                type: "varchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            // Las cuentas que ya estaban en la base reciben un sello propio. Sin esto todas
            // quedarían con la cadena vacía y compartirían el mismo sello, por lo que el sello
            // no distinguiría unas sesiones de otras.
            migrationBuilder.Sql("UPDATE `Usuarios` SET `SelloSesion` = UUID() WHERE `SelloSesion` = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelloSesion",
                table: "Usuarios");
        }
    }
}
