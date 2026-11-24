using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto.Migrations
{

    public partial class SelloDeSesion : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelloSesion",
                table: "Usuarios",
                type: "varchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE `Usuarios` SET `SelloSesion` = UUID() WHERE `SelloSesion` = '';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelloSesion",
                table: "Usuarios");
        }
    }
}
