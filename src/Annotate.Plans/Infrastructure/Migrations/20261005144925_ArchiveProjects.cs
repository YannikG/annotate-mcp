using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Annotate.Plans.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_projects_folder_path",
                table: "projects");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                table: "projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_folder_path",
                table: "projects",
                column: "folder_path",
                unique: true,
                filter: "archived_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_projects_folder_path",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "projects");

            migrationBuilder.CreateIndex(
                name: "IX_projects_folder_path",
                table: "projects",
                column: "folder_path",
                unique: true);
        }
    }
}
