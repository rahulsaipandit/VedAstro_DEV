using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VedAstro.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anonymous_ip_call_records",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anonymous_ip_call_records", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "call_info_statistic",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    RequestUrl = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_call_info_statistic", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "call_tracker",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsRunning = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_call_tracker", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "ip_address_statistic",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    CallsPerSecond = table.Column<double>(type: "double precision", nullable: false),
                    PerSecondTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CallsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    PerMinuteTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CallsPerHour = table.Column<double>(type: "double precision", nullable: false),
                    PerHourTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CallsPerDay = table.Column<double>(type: "double precision", nullable: false),
                    PerDayTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CallsPerMonth = table.Column<double>(type: "double precision", nullable: false),
                    PerMonthTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ip_address_statistic", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "life_event_list",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<string>(type: "text", nullable: false),
                    Nature = table.Column<string>(type: "text", nullable: false),
                    Weight = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_life_event_list", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "open_api_error_book",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Branch = table.Column<string>(type: "text", nullable: false),
                    URL = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_open_api_error_book", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "person_list",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BirthTime = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_list", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "person_share_list",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_share_list", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "raw_request_statistic",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Accept = table.Column<string>(type: "text", nullable: false),
                    AcceptCharset = table.Column<string>(type: "text", nullable: false),
                    AcceptEncoding = table.Column<string>(type: "text", nullable: false),
                    AcceptLanguage = table.Column<string>(type: "text", nullable: false),
                    Authorization = table.Column<string>(type: "text", nullable: false),
                    CacheControl = table.Column<string>(type: "text", nullable: false),
                    Connection = table.Column<string>(type: "text", nullable: false),
                    Cookie = table.Column<string>(type: "text", nullable: false),
                    ContentLength = table.Column<string>(type: "text", nullable: false),
                    ContentMD5 = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<string>(type: "text", nullable: false),
                    Expect = table.Column<string>(type: "text", nullable: false),
                    From = table.Column<string>(type: "text", nullable: false),
                    Host = table.Column<string>(type: "text", nullable: false),
                    IfMatch = table.Column<string>(type: "text", nullable: false),
                    IfModifiedSince = table.Column<string>(type: "text", nullable: false),
                    IfNoneMatch = table.Column<string>(type: "text", nullable: false),
                    IfRange = table.Column<string>(type: "text", nullable: false),
                    IfUnmodifiedSince = table.Column<string>(type: "text", nullable: false),
                    MaxForwards = table.Column<string>(type: "text", nullable: false),
                    Pragma = table.Column<string>(type: "text", nullable: false),
                    ProxyAuthorization = table.Column<string>(type: "text", nullable: false),
                    Range = table.Column<string>(type: "text", nullable: false),
                    Referer = table.Column<string>(type: "text", nullable: false),
                    TE = table.Column<string>(type: "text", nullable: false),
                    Upgrade = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    Via = table.Column<string>(type: "text", nullable: false),
                    Warning = table.Column<string>(type: "text", nullable: false),
                    SecFetchReferer = table.Column<string>(type: "text", nullable: false),
                    SecFetchOrigin = table.Column<string>(type: "text", nullable: false),
                    SecFetchDest = table.Column<string>(type: "text", nullable: false),
                    SecFetchMode = table.Column<string>(type: "text", nullable: false),
                    SecFetchSite = table.Column<string>(type: "text", nullable: false),
                    SecFetchUser = table.Column<string>(type: "text", nullable: false),
                    SecChUaPlatform = table.Column<string>(type: "text", nullable: false),
                    SecChUa = table.Column<string>(type: "text", nullable: false),
                    SecChUaMobile = table.Column<string>(type: "text", nullable: false),
                    SecChUaFullVersion = table.Column<string>(type: "text", nullable: false),
                    SecChUaArch = table.Column<string>(type: "text", nullable: false),
                    SecChUaModel = table.Column<string>(type: "text", nullable: false),
                    SecChUaPlatformVersion = table.Column<string>(type: "text", nullable: false),
                    XAzureClientIP = table.Column<string>(type: "text", nullable: false),
                    XForwardedFor = table.Column<string>(type: "text", nullable: false),
                    XForwardedHost = table.Column<string>(type: "text", nullable: false),
                    XForwardedProto = table.Column<string>(type: "text", nullable: false),
                    XRealIP = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_request_statistic", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "request_url_statistic",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    CallCount = table.Column<double>(type: "double precision", nullable: false),
                    MetadataHash = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_url_statistic", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "subscriber_call_records",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    CallCount = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriber_call_records", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "subscriber_statistic",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    CallCount = table.Column<double>(type: "double precision", nullable: false),
                    MetadataHash = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriber_statistic", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "user_agent_statistic",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    CallCount = table.Column<double>(type: "double precision", nullable: false),
                    MetadataHash = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_agent_statistic", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "user_data_list",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    APIKey = table.Column<string>(type: "text", nullable: false),
                    StripeCustomerID = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_data_list", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "web_page_statistic",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    CallCount = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_page_statistic", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "website_debug_log",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_website_debug_log", x => new { x.partition_key, x.row_key });
                });

            migrationBuilder.CreateTable(
                name: "website_error_log",
                columns: table => new
                {
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    row_key = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: false),
                    StackTrace = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_website_error_log", x => new { x.partition_key, x.row_key });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anonymous_ip_call_records");

            migrationBuilder.DropTable(
                name: "call_info_statistic");

            migrationBuilder.DropTable(
                name: "call_tracker");

            migrationBuilder.DropTable(
                name: "ip_address_statistic");

            migrationBuilder.DropTable(
                name: "life_event_list");

            migrationBuilder.DropTable(
                name: "open_api_error_book");

            migrationBuilder.DropTable(
                name: "person_list");

            migrationBuilder.DropTable(
                name: "person_share_list");

            migrationBuilder.DropTable(
                name: "raw_request_statistic");

            migrationBuilder.DropTable(
                name: "request_url_statistic");

            migrationBuilder.DropTable(
                name: "subscriber_call_records");

            migrationBuilder.DropTable(
                name: "subscriber_statistic");

            migrationBuilder.DropTable(
                name: "user_agent_statistic");

            migrationBuilder.DropTable(
                name: "user_data_list");

            migrationBuilder.DropTable(
                name: "web_page_statistic");

            migrationBuilder.DropTable(
                name: "website_debug_log");

            migrationBuilder.DropTable(
                name: "website_error_log");
        }
    }
}
