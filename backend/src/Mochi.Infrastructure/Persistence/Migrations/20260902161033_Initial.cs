using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mochi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_devices",
                columns: table => new
                {
                    site_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    device_class = table.Column<short>(type: "smallint", nullable: false),
                    browser = table.Column<string>(type: "text", nullable: false),
                    os = table.Column<string>(type: "text", nullable: false),
                    visitors = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_devices", x => new { x.site_id, x.date, x.device_class, x.browser, x.os });
                });

            migrationBuilder.CreateTable(
                name: "daily_events",
                columns: table => new
                {
                    site_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    event_name = table.Column<string>(type: "text", nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<short>(type: "smallint", nullable: false),
                    total = table.Column<int>(type: "integer", nullable: false),
                    unique_visitors = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_events", x => new { x.site_id, x.date, x.event_name, x.path, x.channel });
                });

            migrationBuilder.CreateTable(
                name: "daily_geo",
                columns: table => new
                {
                    site_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    visitors = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_geo", x => new { x.site_id, x.date, x.country });
                });

            migrationBuilder.CreateTable(
                name: "daily_pages",
                columns: table => new
                {
                    site_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    visitors = table.Column<int>(type: "integer", nullable: false),
                    pageviews = table.Column<int>(type: "integer", nullable: false),
                    entries = table.Column<int>(type: "integer", nullable: false),
                    exits = table.Column<int>(type: "integer", nullable: false),
                    bounced_sessions = table.Column<int>(type: "integer", nullable: false),
                    total_duration_sec = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_pages", x => new { x.site_id, x.date, x.path });
                });

            migrationBuilder.CreateTable(
                name: "daily_site_stats",
                columns: table => new
                {
                    site_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    visitors = table.Column<int>(type: "integer", nullable: false),
                    pageviews = table.Column<int>(type: "integer", nullable: false),
                    sessions = table.Column<int>(type: "integer", nullable: false),
                    bounced_sessions = table.Column<int>(type: "integer", nullable: false),
                    total_session_duration_sec = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_site_stats", x => new { x.site_id, x.date });
                });

            migrationBuilder.CreateTable(
                name: "daily_sources",
                columns: table => new
                {
                    site_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    channel = table.Column<short>(type: "smallint", nullable: false),
                    referrer_domain = table.Column<string>(type: "text", nullable: false),
                    campaign = table.Column<string>(type: "text", nullable: false),
                    visitors = table.Column<int>(type: "integer", nullable: false),
                    pageviews = table.Column<int>(type: "integer", nullable: false),
                    bounced_sessions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_sources", x => new { x.site_id, x.date, x.channel, x.referrer_domain, x.campaign });
                });

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    retention = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sites", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    visitor_hash = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    event_name = table.Column<string>(type: "text", nullable: true),
                    referrer_domain = table.Column<string>(type: "text", nullable: true),
                    channel = table.Column<short>(type: "smallint", nullable: false),
                    campaign = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    device_class = table.Column<short>(type: "smallint", nullable: false),
                    browser = table.Column<string>(type: "text", nullable: false),
                    os = table.Column<string>(type: "text", nullable: false),
                    ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_events_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goals",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    site_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    target = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goals", x => x.id);
                    table.ForeignKey(
                        name: "FK_goals_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_site_id_ts",
                table: "events",
                columns: new[] { "site_id", "ts" });

            migrationBuilder.CreateIndex(
                name: "IX_goals_site_id",
                table: "goals",
                column: "site_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_devices");

            migrationBuilder.DropTable(
                name: "daily_events");

            migrationBuilder.DropTable(
                name: "daily_geo");

            migrationBuilder.DropTable(
                name: "daily_pages");

            migrationBuilder.DropTable(
                name: "daily_site_stats");

            migrationBuilder.DropTable(
                name: "daily_sources");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "goals");

            migrationBuilder.DropTable(
                name: "sites");
        }
    }
}
