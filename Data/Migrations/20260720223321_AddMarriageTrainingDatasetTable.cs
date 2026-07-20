using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VedAstro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarriageTrainingDatasetTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "marriage_training_dataset",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    MarriageDate = table.Column<string>(type: "text", nullable: false),
                    ChildrenData = table.Column<string>(type: "text", nullable: false),
                    DivorceData = table.Column<string>(type: "text", nullable: false),
                    DivorceDate = table.Column<string>(type: "text", nullable: false),
                    MalePersonId = table.Column<string>(type: "text", nullable: false),
                    FemalePersonId = table.Column<string>(type: "text", nullable: false),
                    Embeddings = table.Column<string>(type: "text", nullable: false),
                    KutaScore = table.Column<double>(type: "double precision", nullable: false),
                    MarriageDuration = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marriage_training_dataset", x => new { x.partition_key, x.row_key });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marriage_training_dataset");
        }
    }
}
