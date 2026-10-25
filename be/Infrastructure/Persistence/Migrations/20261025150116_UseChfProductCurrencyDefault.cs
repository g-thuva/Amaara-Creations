using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace be.Migrations
{
    /// <inheritdoc />
    public partial class UseChfProductCurrencyDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Products",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "CHF",
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3,
                oldDefaultValue: "LKR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Products",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LKR",
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3,
                oldDefaultValue: "CHF");
        }
    }
}
