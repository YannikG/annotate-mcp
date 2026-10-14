using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Annotate.Plans.Infrastructure.Migrations
{
    /// <inheritdoc />
    partial class Documents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_revisions_plan_id_number",
                table: "revisions");

            migrationBuilder.AlterColumn<string>(
                name: "plan_id",
                table: "revisions",
                type: "TEXT",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 36);

            migrationBuilder.AddColumn<string>(
                name: "document_id",
                table: "revisions",
                type: "TEXT",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    project_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    session_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    read_only_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_documents_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "block_locks",
                columns: table => new
                {
                    document_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    block_key = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_block_locks", x => new { x.document_id, x.block_key });
                    table.ForeignKey(
                        name: "FK_block_locks_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_revisions_document_id_number",
                table: "revisions",
                columns: new[] { "document_id", "number" },
                unique: true,
                filter: "document_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_revisions_plan_id_number",
                table: "revisions",
                columns: new[] { "plan_id", "number" },
                unique: true,
                filter: "plan_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_documents_project_id",
                table: "documents",
                column: "project_id");

            migrationBuilder.AddForeignKey(
                name: "FK_revisions_documents_document_id",
                table: "revisions",
                column: "document_id",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_revisions_documents_document_id",
                table: "revisions");

            migrationBuilder.DropTable(
                name: "block_locks");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropIndex(
                name: "IX_revisions_document_id_number",
                table: "revisions");

            migrationBuilder.DropIndex(
                name: "IX_revisions_plan_id_number",
                table: "revisions");

            migrationBuilder.DropColumn(
                name: "document_id",
                table: "revisions");

            migrationBuilder.AlterColumn<string>(
                name: "plan_id",
                table: "revisions",
                type: "TEXT",
                maxLength: 36,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_revisions_plan_id_number",
                table: "revisions",
                columns: new[] { "plan_id", "number" },
                unique: true);
        }
    }
}
