using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Annotate.Plans.Infrastructure.Migrations
{
    /// <inheritdoc />
    partial class ArchivePlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                table: "plans",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "plans");
        }
    }
}
