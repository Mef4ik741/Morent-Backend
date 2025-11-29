using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCarBookingShadowKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BalanceTransactions_Users_UserId",
                table: "BalanceTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BalanceTransactions",
                table: "BalanceTransactions");

            migrationBuilder.RenameTable(
                name: "BalanceTransactions",
                newName: "BalanceTransaction");

            migrationBuilder.RenameIndex(
                name: "IX_BalanceTransactions_UserId",
                table: "BalanceTransaction",
                newName: "IX_BalanceTransaction_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BalanceTransaction",
                table: "BalanceTransaction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BalanceTransaction_Users_UserId",
                table: "BalanceTransaction",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BalanceTransaction_Users_UserId",
                table: "BalanceTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BalanceTransaction",
                table: "BalanceTransaction");

            migrationBuilder.RenameTable(
                name: "BalanceTransaction",
                newName: "BalanceTransactions");

            migrationBuilder.RenameIndex(
                name: "IX_BalanceTransaction_UserId",
                table: "BalanceTransactions",
                newName: "IX_BalanceTransactions_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BalanceTransactions",
                table: "BalanceTransactions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BalanceTransactions_Users_UserId",
                table: "BalanceTransactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
