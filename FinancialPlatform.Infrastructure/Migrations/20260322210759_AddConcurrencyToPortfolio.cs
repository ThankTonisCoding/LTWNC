using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyToPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Portfolios",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Portfolios");
        }
    }
}
