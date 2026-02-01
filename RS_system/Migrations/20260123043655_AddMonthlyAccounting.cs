using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RS_system.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reportes_mensuales_contables",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grupo_trabajo_id = table.Column<long>(type: "bigint", nullable: false),
                    mes = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    saldo_inicial = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cerrado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reportes_mensuales_contables", x => x.id);
                    table.ForeignKey(
                        name: "FK_reportes_mensuales_contables_grupos_trabajo_grupo_trabajo_id",
                        column: x => x.grupo_trabajo_id,
                        principalTable: "grupos_trabajo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contabilidad_registros",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reporte_mensual_id = table.Column<long>(type: "bigint", nullable: true),
                    grupo_trabajo_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contabilidad_registros", x => x.id);
                    table.ForeignKey(
                        name: "FK_contabilidad_registros_grupos_trabajo_grupo_trabajo_id",
                        column: x => x.grupo_trabajo_id,
                        principalTable: "grupos_trabajo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contabilidad_registros_reportes_mensuales_contables_reporte_mensual_id",
                        column: x => x.reporte_mensual_id,
                        principalTable: "reportes_mensuales_contables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contabilidad_registros_grupo_trabajo_id",
                table: "contabilidad_registros",
                column: "grupo_trabajo_id");

            migrationBuilder.CreateIndex(
                name: "IX_contabilidad_registros_reporte_mensual_id",
                table: "contabilidad_registros",
                column: "reporte_mensual_id");

            migrationBuilder.CreateIndex(
                name: "IX_reportes_mensuales_contables_grupo_trabajo_id",
                table: "reportes_mensuales_contables",
                column: "grupo_trabajo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contabilidad_registros");

            migrationBuilder.DropTable(
                name: "reportes_mensuales_contables");
        }

    }
}
