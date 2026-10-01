using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AsistenciaCore.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaTerminalesKiosco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TerminalesKiosco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    NumeroSerie = table.Column<string>(type: "text", nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimaConexion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    GeocercaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminalesKiosco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerminalesKiosco_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TerminalesKiosco_Geocercas_GeocercaId",
                        column: x => x.GeocercaId,
                        principalTable: "Geocercas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TerminalesKiosco_EmpresaId",
                table: "TerminalesKiosco",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_TerminalesKiosco_GeocercaId",
                table: "TerminalesKiosco",
                column: "GeocercaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerminalesKiosco");
        }
    }
}
