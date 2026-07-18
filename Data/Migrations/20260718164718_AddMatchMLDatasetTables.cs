using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VedAstro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchMLDatasetTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "body_info_dataset",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Info = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_body_info_dataset", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "marriage_info_dataset",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Info = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marriage_info_dataset", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "person_name_embeddings",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Embeddings = table.Column<string>(type: "text", nullable: false),
                    PersonId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_name_embeddings", x => new { x.partition_key, x.row_key });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "body_info_dataset");

            migrationBuilder.DropTable(
                name: "marriage_info_dataset");

            migrationBuilder.DropTable(
                name: "person_name_embeddings");
        }
    }
}
