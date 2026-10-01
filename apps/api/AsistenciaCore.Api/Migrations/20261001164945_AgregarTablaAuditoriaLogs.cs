using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AsistenciaCore.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaAuditoriaLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriaLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Accion = table.Column<string>(type: "text", nullable: false),
                    Detalle = table.Column<string>(type: "text", nullable: false),
                    FechaHoraUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Usuario = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaLogs");
        }
    }
}
