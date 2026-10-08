using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Annotate.Reviews.Migrations
{
    /// <inheritdoc />
    public partial class BlockComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "accepted",
                table: "annotations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "author",
                table: "annotations",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "operator");

            migrationBuilder.AddColumn<string>(
                name: "block_key",
                table: "annotations",
                type: "TEXT",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "annotation_replies",
                columns: table => new
                {
                    review_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    annotation_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    reply_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    created_at = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annotation_replies", x => new { x.review_id, x.annotation_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_annotation_replies_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "annotation_replies");

            migrationBuilder.DropColumn(
                name: "accepted",
                table: "annotations");

            migrationBuilder.DropColumn(
                name: "author",
                table: "annotations");

            migrationBuilder.DropColumn(
                name: "block_key",
                table: "annotations");
        }
    }
}
