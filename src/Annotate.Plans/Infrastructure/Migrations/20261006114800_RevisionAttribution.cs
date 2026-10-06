using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Annotate.Plans.Infrastructure.Migrations
{
    /// <inheritdoc />
    partial class RevisionAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agent",
                table: "revisions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_name",
                table: "revisions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_version",
                table: "revisions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model",
                table: "revisions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agent",
                table: "revisions");

            migrationBuilder.DropColumn(
                name: "client_name",
                table: "revisions");

            migrationBuilder.DropColumn(
                name: "client_version",
                table: "revisions");

            migrationBuilder.DropColumn(
                name: "model",
                table: "revisions");
        }
    }
}
