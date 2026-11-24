using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Proyecto.Migrations
{

    public partial class CreacionInicial : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Contrasena = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "date", nullable: false),
                    EsAdmin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EstaActivo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Directorios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    PadreId = table.Column<long>(type: "bigint", nullable: true),
                    PropietarioId = table.Column<long>(type: "bigint", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Directorios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Directorios_Directorios_PadreId",
                        column: x => x.PadreId,
                        principalTable: "Directorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Directorios_Usuarios_PropietarioId",
                        column: x => x.PropietarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TokensSesion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    Token = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    ExpiraEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensSesion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensSesion_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Archivos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Extension = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    NombreAlmacenamiento = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    TipoContenido = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    DirectorioId = table.Column<long>(type: "bigint", nullable: false),
                    PropietarioId = table.Column<long>(type: "bigint", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Archivos_Directorios_DirectorioId",
                        column: x => x.DirectorioId,
                        principalTable: "Directorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Archivos_Usuarios_PropietarioId",
                        column: x => x.PropietarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Archivos_DirectorioId_Nombre",
                table: "Archivos",
                columns: new[] { "DirectorioId", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Archivos_NombreAlmacenamiento",
                table: "Archivos",
                column: "NombreAlmacenamiento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Archivos_PropietarioId",
                table: "Archivos",
                column: "PropietarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Directorios_PadreId_Nombre",
                table: "Directorios",
                columns: new[] { "PadreId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Directorios_PropietarioId",
                table: "Directorios",
                column: "PropietarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensSesion_Token",
                table: "TokensSesion",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensSesion_UsuarioId",
                table: "TokensSesion",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Archivos");

            migrationBuilder.DropTable(
                name: "TokensSesion");

            migrationBuilder.DropTable(
                name: "Directorios");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
