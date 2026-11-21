using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Proyecto.Migrations
{
    /// <inheritdoc />
    public partial class EquiposYCompartidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL exige conservar un índice que empiece por PadreId para la FK del árbol.
            migrationBuilder.CreateIndex(
                name: "IX_Directorios_PadreId",
                table: "Directorios",
                column: "PadreId");

            migrationBuilder.DropIndex(
                name: "IX_Directorios_PadreId_Nombre",
                table: "Directorios");

            migrationBuilder.AddColumn<long>(
                name: "EquipoId",
                table: "Usuarios",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "NivelAdmin",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "EquipoId",
                table: "Directorios",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CodigoRegistro = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ContenidosCompartidos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EquipoDestinoId = table.Column<long>(type: "bigint", nullable: false),
                    ArchivoId = table.Column<long>(type: "bigint", nullable: true),
                    DirectorioId = table.Column<long>(type: "bigint", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContenidosCompartidos", x => x.Id);
                    table.CheckConstraint("CK_Compartido_UnRecurso", "(`ArchivoId` IS NULL) <> (`DirectorioId` IS NULL)");
                    table.ForeignKey(
                        name: "FK_ContenidosCompartidos_Archivos_ArchivoId",
                        column: x => x.ArchivoId,
                        principalTable: "Archivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContenidosCompartidos_Directorios_DirectorioId",
                        column: x => x.DirectorioId,
                        principalTable: "Directorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContenidosCompartidos_Equipos_EquipoDestinoId",
                        column: x => x.EquipoDestinoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EquipoId",
                table: "Usuarios",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Directorios_EquipoId_PadreId_Nombre",
                table: "Directorios",
                columns: new[] { "EquipoId", "PadreId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContenidosCompartidos_ArchivoId",
                table: "ContenidosCompartidos",
                column: "ArchivoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContenidosCompartidos_DirectorioId",
                table: "ContenidosCompartidos",
                column: "DirectorioId");

            migrationBuilder.CreateIndex(
                name: "IX_ContenidosCompartidos_EquipoDestinoId_ArchivoId",
                table: "ContenidosCompartidos",
                columns: new[] { "EquipoDestinoId", "ArchivoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContenidosCompartidos_EquipoDestinoId_DirectorioId",
                table: "ContenidosCompartidos",
                columns: new[] { "EquipoDestinoId", "DirectorioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_CodigoRegistro",
                table: "Equipos",
                column: "CodigoRegistro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_Nombre",
                table: "Equipos",
                column: "Nombre",
                unique: true);

            // Conserva usuarios, árbol y referencias a los archivos existentes.
            migrationBuilder.Sql("INSERT INTO Equipos (Id, Nombre, CodigoRegistro, CreadoEn) VALUES (1, 'Equipo principal', REPLACE(UUID(), '-', ''), UTC_TIMESTAMP(6));");
            migrationBuilder.Sql("UPDATE Usuarios SET EquipoId = 1, NivelAdmin = CASE WHEN EsAdmin = 1 THEN 1 ELSE 0 END;");
            migrationBuilder.Sql("UPDATE Directorios SET EquipoId = 1;");
            // Solo el administrador activo más antiguo pasa a nivel 2.
            migrationBuilder.Sql("UPDATE Usuarios u JOIN (SELECT MIN(Id) AS Id FROM Usuarios WHERE EsAdmin = 1 AND EstaActivo = 1) primero ON primero.Id = u.Id SET u.NivelAdmin = 2;");

            migrationBuilder.AddForeignKey(
                name: "FK_Directorios_Equipos_EquipoId",
                table: "Directorios",
                column: "EquipoId",
                principalTable: "Equipos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Equipos_EquipoId",
                table: "Usuarios",
                column: "EquipoId",
                principalTable: "Equipos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Directorios_Equipos_EquipoId",
                table: "Directorios");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Equipos_EquipoId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "ContenidosCompartidos");

            migrationBuilder.DropTable(
                name: "Equipos");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_EquipoId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Directorios_EquipoId_PadreId_Nombre",
                table: "Directorios");

            migrationBuilder.CreateIndex(
                name: "IX_Directorios_PadreId_Nombre",
                table: "Directorios",
                columns: new[] { "PadreId", "Nombre" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_Directorios_PadreId",
                table: "Directorios");

            migrationBuilder.DropColumn(
                name: "EquipoId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NivelAdmin",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EquipoId",
                table: "Directorios");


        }
    }
}
