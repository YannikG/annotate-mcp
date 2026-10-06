using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Annotate.Plans.Migrations
{
    /// <inheritdoc />
    public partial class StoreBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blocks",
                columns: table => new
                {
                    revision_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    block_key = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    section_path = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    source_start = table.Column<int>(type: "INTEGER", nullable: false),
                    source_end = table.Column<int>(type: "INTEGER", nullable: false),
                    content_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocks", x => new { x.revision_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_blocks_revisions_revision_id",
                        column: x => x.revision_id,
                        principalTable: "revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocks");
        }
    }
}
