using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuthPasswordAndRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing owner rows cannot survive this migration: the Pin* columns are
            // being dropped, and the hashing algorithm changes from a single salted
            // SHA-256 pass to PBKDF2, so an old hash could not be verified even if it
            // were copied across. Clearing the table lets DatabaseSeeder recreate the
            // account with the new algorithm on the next startup.
            //
            // Safe because this is a single-tenant demo system whose owner account is
            // defined in source (DatabaseSeeder.OwnerEmail / OwnerPassword), not
            // user-generated data. RefreshTokens cascade with the owner.
            migrationBuilder.Sql("DELETE FROM OwnerAccounts;");

            migrationBuilder.DropColumn(
                name: "PinHash",
                table: "OwnerAccounts");

            migrationBuilder.DropColumn(
                name: "PinSalt",
                table: "OwnerAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "OwnerAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "OwnerAccounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordSalt",
                table: "OwnerAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_OwnerAccounts_OwnerAccountId",
                        column: x => x.OwnerAccountId,
                        principalTable: "OwnerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerAccounts_Email",
                table: "OwnerAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_OwnerAccountId",
                table: "RefreshTokens",
                column: "OwnerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_OwnerAccounts_Email",
                table: "OwnerAccounts");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "OwnerAccounts");

            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                table: "OwnerAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "OwnerAccounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<string>(
                name: "PinHash",
                table: "OwnerAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PinSalt",
                table: "OwnerAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
