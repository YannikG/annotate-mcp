using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Annotate.Reviews.Migrations
{
    /// <inheritdoc />
    public partial class InitialReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    revision_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 18, nullable: false),
                    feedback = table.Column<string>(type: "TEXT", maxLength: 1500000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "annotations",
                columns: table => new
                {
                    review_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    annotation_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    block_ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    start_offset = table.Column<int>(type: "INTEGER", nullable: false),
                    end_offset = table.Column<int>(type: "INTEGER", nullable: false),
                    text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    replacement = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    comment = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    created_at = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annotations", x => new { x.review_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_annotations_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "decision_answers",
                columns: table => new
                {
                    review_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    fence_id = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    answer = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    is_other = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_answers", x => new { x.review_id, x.fence_id });
                    table.ForeignKey(
                        name: "FK_decision_answers_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_decisions",
                columns: table => new
                {
                    review_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    fence_id = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    kind = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    prompt = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_decisions", x => new { x.review_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_review_decisions_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_options",
                columns: table => new
                {
                    review_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    fence_id = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    label = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_options", x => new { x.review_id, x.fence_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_review_options_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_review_decisions_review_id_fence_id",
                table: "review_decisions",
                columns: new[] { "review_id", "fence_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_revision_id",
                table: "reviews",
                column: "revision_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "annotations");

            migrationBuilder.DropTable(
                name: "decision_answers");

            migrationBuilder.DropTable(
                name: "review_decisions");

            migrationBuilder.DropTable(
                name: "review_options");

            migrationBuilder.DropTable(
                name: "reviews");
        }
    }
}
