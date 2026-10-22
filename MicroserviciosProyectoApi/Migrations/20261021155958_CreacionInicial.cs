using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Proyecto.Migrations
{
    /// <summary>
    /// Migración inicial del repositorio de archivos (nombre interno: CreacionInicial).
    ///
    /// El nombre del archivo lleva el prefijo 20261021155958 porque es una marca de tiempo
    /// (año mes día hora minuto segundo, en UTC) que Entity Framework Core genera de forma
    /// obligatoria. Ese número es lo que le dice a EF en qué orden debe aplicar las
    /// migraciones; si se quitara, EF no podría ordenarlas y el esquema podría quedar
    /// inconsistente. Lo que sí se tradujo al español fue el nombre descriptivo
    /// ("CreacionInicial") y todo el contenido.
    ///
    /// Crea el esquema completo de la base de datos "ProyectoM":
    ///   - Usuarios      : cuentas de usuario (módulo 2).
    ///   - TokensSesion  : un token de refresco vigente por usuario (módulo 2).
    ///   - Directorios   : árbol de directorios (módulo 3.1).
    ///   - Archivos      : archivos subidos, sólo dentro de un directorio (módulo 3.2).
    ///
    /// El orden de creación respeta las dependencias: primero las tablas que no
    /// dependen de nadie (Usuarios), luego las que dependen de ellas.
    /// </summary>
    public partial class CreacionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fija utf8mb4 como charset de la base para admitir acentos, emojis, etc.
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            // ------------------------------------------------------------------
            // Tabla Usuarios: cuentas del sistema. No depende de ninguna otra tabla.
            // Contrasena guarda el hash BCrypt (nunca la contraseña en claro).
            // EstaActivo = false por defecto al registrarse; un administrador lo activa.
            // ------------------------------------------------------------------
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

            // ------------------------------------------------------------------
            // Tabla Directorios: cada fila es un directorio del repositorio.
            // PadreId = NULL significa que es un directorio raíz (nivel superior).
            // Dos llaves foráneas:
            //   - PadreId -> Directorios.Id : arma el árbol. Se usa Restrict (no Cascade)
            //     porque MySQL no admite el borrado en cascada sobre una llave foránea que
            //     se apunta a sí misma; el borrado del subárbol se hace en el controlador.
            //   - PropietarioId -> Usuarios.Id : creador; si se borra el usuario, sus
            //     directorios se eliminan en cascada.
            // ------------------------------------------------------------------
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

            // ------------------------------------------------------------------
            // Tabla TokensSesion: guarda el hash SHA-256 del token de refresco vigente
            // de cada usuario (nunca el token en claro, por seguridad y porque el JWT
            // completo supera el tamaño razonable de una columna).
            // La llave foránea UsuarioId es única, de modo que un usuario sólo puede
            // tener un token de refresco activo a la vez (rotación en cada inicio de
            // sesión y en cada renovación).
            // ------------------------------------------------------------------
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

            // ------------------------------------------------------------------
            // Tabla Archivos: archivos del repositorio. Siempre pertenecen a un
            // directorio (DirectorioId obligatorio, no puede haber archivos sueltos).
            //   - Nombre               : nombre visible SIN extensión (permite renombrar
            //                            sin cambiar el tipo de archivo, requisito 3.4).
            //   - Extension            : extensión con el punto (".pdf", ".jpg", ...).
            //   - NombreAlmacenamiento : nombre físico real en disco (GUID + extensión),
            //                            único, para que dos archivos con el mismo nombre
            //                            no colisionen.
            // ------------------------------------------------------------------
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

            // Índice que acelera listar los archivos de un directorio por nombre.
            migrationBuilder.CreateIndex(
                name: "IX_Archivos_DirectorioId_Nombre",
                table: "Archivos",
                columns: new[] { "DirectorioId", "Nombre" });

            // El nombre físico (GUID) es único por diseño.
            migrationBuilder.CreateIndex(
                name: "IX_Archivos_NombreAlmacenamiento",
                table: "Archivos",
                column: "NombreAlmacenamiento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Archivos_PropietarioId",
                table: "Archivos",
                column: "PropietarioId");

            // Unicidad de nombre por nivel: no puede haber dos directorios con el
            // mismo nombre dentro del mismo padre (requisito 3.1). Ojo: en MySQL
            // los NULL se consideran distintos entre sí, así que este índice no
            // cubre el caso de la raíz (PadreId NULL); ese caso se valida en el
            // controlador antes de insertar.
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

            // Un token de refresco por usuario (índice único).
            migrationBuilder.CreateIndex(
                name: "IX_TokensSesion_UsuarioId",
                table: "TokensSesion",
                column: "UsuarioId",
                unique: true);

            // Un correo por usuario (índice único).
            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);
        }

        /// <summary>
        /// Revierte la migración. Se eliminan primero las tablas que dependen de
        /// otras (Archivos y Directorios) y al final Usuarios, respetando las llaves foráneas.
        /// </summary>
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
