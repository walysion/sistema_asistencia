using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AsistenciaCore.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaMarcaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Marcaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpleadoId = table.Column<int>(type: "integer", nullable: false),
                    FechaHoraServidor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraDispositivo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TipoMovimiento = table.Column<string>(type: "text", nullable: false),
                    Origen = table.Column<string>(type: "text", nullable: false),
                    Latitud = table.Column<double>(type: "double precision", nullable: true),
                    Longitud = table.Column<double>(type: "double precision", nullable: true),
                    FueraDeGeocerca = table.Column<bool>(type: "boolean", nullable: false),
                    EsSincronizacionOffline = table.Column<bool>(type: "boolean", nullable: false),
                    DispositivoId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marcaciones_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Marcaciones_EmpleadoId",
                table: "Marcaciones",
                column: "EmpleadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Marcaciones");
        }
    }
}
