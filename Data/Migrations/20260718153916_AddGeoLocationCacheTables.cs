using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VedAstro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeoLocationCacheTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "address_geo_location",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    MetadataHash = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_address_geo_location", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "coordinates_geo_location",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MetadataHash = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coordinates_geo_location", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "geo_location_timezone",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    TimezoneText = table.Column<string>(type: "text", nullable: false),
                    MetadataHash = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geo_location_timezone", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "geo_location_timezone_metadata",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    TimezoneText = table.Column<string>(type: "text", nullable: false),
                    StandardOffset = table.Column<string>(type: "text", nullable: false),
                    DaylightSavings = table.Column<string>(type: "text", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    Standard_Name = table.Column<string>(type: "text", nullable: false),
                    Daylight_Name = table.Column<string>(type: "text", nullable: false),
                    ISO_Name = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geo_location_timezone_metadata", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "ip_address_geo_location",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    LocationName = table.Column<string>(type: "text", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    MetadataHash = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ip_address_geo_location", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "ip_address_geo_location_metadata",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AsnName = table.Column<string>(type: "text", nullable: false),
                    TimezoneName = table.Column<string>(type: "text", nullable: false),
                    TimezoneOffset = table.Column<string>(type: "text", nullable: false),
                    IsProxy = table.Column<string>(type: "text", nullable: false),
                    IsDatacenter = table.Column<string>(type: "text", nullable: false),
                    IsAnonymous = table.Column<string>(type: "text", nullable: false),
                    IsKnownAttacker = table.Column<string>(type: "text", nullable: false),
                    IsKnownAbuser = table.Column<string>(type: "text", nullable: false),
                    IsThreat = table.Column<string>(type: "text", nullable: false),
                    IsBogon = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ip_address_geo_location_metadata", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "search_address_geo_location",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Results = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_address_geo_location", x => new { x.partition_key, x.row_key });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "address_geo_location");

            migrationBuilder.DropTable(
                name: "coordinates_geo_location");

            migrationBuilder.DropTable(
                name: "geo_location_timezone");

            migrationBuilder.DropTable(
                name: "geo_location_timezone_metadata");

            migrationBuilder.DropTable(
                name: "ip_address_geo_location");

            migrationBuilder.DropTable(
                name: "ip_address_geo_location_metadata");

            migrationBuilder.DropTable(
                name: "search_address_geo_location");
        }
    }
}
