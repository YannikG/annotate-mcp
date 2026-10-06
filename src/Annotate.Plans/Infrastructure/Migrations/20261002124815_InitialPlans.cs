using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Annotate.Plans.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    folder_path = table.Column<string>(type: "TEXT", nullable: true),
                    display_name = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    project_id = table.Column<string>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    session_id = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.id);
                    table.ForeignKey(
                        name: "FK_plans_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "revisions",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    plan_id = table.Column<string>(type: "TEXT", nullable: false),
                    number = table.Column<int>(type: "INTEGER", nullable: false),
                    parent_revision_id = table.Column<string>(type: "TEXT", nullable: true),
                    markdown = table.Column<string>(type: "TEXT", nullable: false),
                    summary = table.Column<string>(type: "TEXT", nullable: true),
                    story_url = table.Column<string>(type: "TEXT", nullable: true),
                    acceptance_criteria = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_revisions_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_revisions_revisions_parent_revision_id",
                        column: x => x.parent_revision_id,
                        principalTable: "revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plans_project_id",
                table: "plans",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_folder_path",
                table: "projects",
                column: "folder_path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revisions_parent_revision_id",
                table: "revisions",
                column: "parent_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_revisions_plan_id_number",
                table: "revisions",
                columns: new[] { "plan_id", "number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "revisions");

            migrationBuilder.DropTable(
                name: "plans");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
