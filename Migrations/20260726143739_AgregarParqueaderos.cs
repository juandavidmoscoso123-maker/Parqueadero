using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parquing.Migrations
{
    /// <inheritdoc />
    public partial class AgregarParqueaderos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParqueaderoId",
                table: "Vehiculos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Parqueaderos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parqueaderos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_ParqueaderoId",
                table: "Vehiculos",
                column: "ParqueaderoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehiculos_Parqueaderos_ParqueaderoId",
                table: "Vehiculos",
                column: "ParqueaderoId",
                principalTable: "Parqueaderos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehiculos_Parqueaderos_ParqueaderoId",
                table: "Vehiculos");

            migrationBuilder.DropTable(
                name: "Parqueaderos");

            migrationBuilder.DropIndex(
                name: "IX_Vehiculos_ParqueaderoId",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "ParqueaderoId",
                table: "Vehiculos");
        }
    }
}
