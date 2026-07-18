using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VedAstro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_message",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Sender = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    MessageNumber = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_message", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "preset_question_embeddings",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Embeddings = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preset_question_embeddings", x => new { x.partition_key, x.row_key });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_message");

            migrationBuilder.DropTable(
                name: "preset_question_embeddings");
        }
    }
}
