using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertRankToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Меняем тип колонки Rank с int на string
            migrationBuilder.AlterColumn<string>(
                name: "Rank",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Beginner'      WHERE \"Rank\" = '0';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Verified'      WHERE \"Rank\" = '1';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Trusted'       WHERE \"Rank\" = '2';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Reliable'      WHERE \"Rank\" = '3';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Respected'     WHERE \"Rank\" = '4';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Endorsed'      WHERE \"Rank\" = '5';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Elite'         WHERE \"Rank\" = '6';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Distinguished' WHERE \"Rank\" = '7';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Veteran'       WHERE \"Rank\" = '8';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Ambassador'    WHERE \"Rank\" = '9';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Legendary'     WHERE \"Rank\" = '10';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Fraudulent'    WHERE \"Rank\" = '11';");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Rank\" = 'Banned'        WHERE \"Rank\" = '12';");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Rank",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
