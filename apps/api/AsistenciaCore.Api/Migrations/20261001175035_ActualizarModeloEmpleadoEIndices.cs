using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsistenciaCore.Api.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarModeloEmpleadoEIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Marcaciones_EmpleadoId",
                table: "Marcaciones");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_EmpresaId",
                table: "Empleados");

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "Empleados",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaContratacion",
                table: "Empleados",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutDni",
                table: "Empleados",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TerminalesKiosco_ApiKey",
                table: "TerminalesKiosco",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marcaciones_EmpleadoId_FechaHoraServidor",
                table: "Marcaciones",
                columns: new[] { "EmpleadoId", "FechaHoraServidor" });

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_EmpresaId_CodigoTrabajador",
                table: "Empleados",
                columns: new[] { "EmpresaId", "CodigoTrabajador" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_TerminalesKiosco_ApiKey",
                table: "TerminalesKiosco");

            migrationBuilder.DropIndex(
                name: "IX_Marcaciones_EmpleadoId_FechaHoraServidor",
                table: "Marcaciones");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_EmpresaId_CodigoTrabajador",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "FechaContratacion",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "RutDni",
                table: "Empleados");

            migrationBuilder.CreateIndex(
                name: "IX_Marcaciones_EmpleadoId",
                table: "Marcaciones",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_EmpresaId",
                table: "Empleados",
                column: "EmpresaId");
        }
    }
}
