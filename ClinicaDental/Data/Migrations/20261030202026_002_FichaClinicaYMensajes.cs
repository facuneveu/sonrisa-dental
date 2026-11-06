using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaDental.Data.Migrations
{
    /// <inheritdoc />
    public partial class _002_FichaClinicaYMensajes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RutaArchivo",
                table: "Adjuntos");

            migrationBuilder.AddColumn<byte[]>(
                name: "Contenido",
                table: "Adjuntos",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "Mensajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OdontologoId = table.Column<int>(type: "int", nullable: false),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    TurnoId = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Asunto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaLectura = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensajes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mensajes_Odontologos_OdontologoId",
                        column: x => x.OdontologoId,
                        principalTable: "Odontologos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mensajes_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mensajes_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_OdontologoId",
                table: "Mensajes",
                column: "OdontologoId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_PacienteId_FechaEnvio",
                table: "Mensajes",
                columns: new[] { "PacienteId", "FechaEnvio" });

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_TurnoId",
                table: "Mensajes",
                column: "TurnoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mensajes");

            migrationBuilder.DropColumn(
                name: "Contenido",
                table: "Adjuntos");

            migrationBuilder.AddColumn<string>(
                name: "RutaArchivo",
                table: "Adjuntos",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }
    }
}
