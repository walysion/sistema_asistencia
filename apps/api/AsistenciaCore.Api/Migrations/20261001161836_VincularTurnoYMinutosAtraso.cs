using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsistenciaCore.Api.Migrations
{
    /// <inheritdoc />
    public partial class VincularTurnoYMinutosAtraso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinutosAtraso",
                table: "Marcaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TurnoId",
                table: "Empleados",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_TurnoId",
                table: "Empleados",
                column: "TurnoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Empleados_Turnos_TurnoId",
                table: "Empleados",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_Turnos_TurnoId",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_TurnoId",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "MinutosAtraso",
                table: "Marcaciones");

            migrationBuilder.DropColumn(
                name: "TurnoId",
                table: "Empleados");
        }
    }
}
