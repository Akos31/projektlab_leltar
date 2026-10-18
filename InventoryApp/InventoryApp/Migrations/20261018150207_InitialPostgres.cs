using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResponsiblePersons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponsiblePersons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryZoneId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryPeriods_InventoryZones_InventoryZoneId",
                        column: x => x.InventoryZoneId,
                        principalTable: "InventoryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomNumber = table.Column<string>(type: "text", nullable: false),
                    InventoryZoneId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_InventoryZones_InventoryZoneId",
                        column: x => x.InventoryZoneId,
                        principalTable: "InventoryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StatusChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PurchaseValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NetBookValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AssetTypeId = table.Column<int>(type: "integer", nullable: false),
                    InventoryZoneId = table.Column<int>(type: "integer", nullable: false),
                    ResponsiblePersonId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_AssetTypes_AssetTypeId",
                        column: x => x.AssetTypeId,
                        principalTable: "AssetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assets_InventoryZones_InventoryZoneId",
                        column: x => x.InventoryZoneId,
                        principalTable: "InventoryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assets_ResponsiblePersons_ResponsiblePersonId",
                        column: x => x.ResponsiblePersonId,
                        principalTable: "ResponsiblePersons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ResponsiblePersonId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_ResponsiblePersons_ResponsiblePersonId",
                        column: x => x.ResponsiblePersonId,
                        principalTable: "ResponsiblePersons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Accessories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    SubNumber = table.Column<int>(type: "integer", nullable: false),
                    VerifiedDirectly = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accessories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accessories_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    CodeValue = table.Column<string>(type: "text", nullable: false),
                    CodeType = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetCodes_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    IsOtherZone = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    InventoryPeriodId = table.Column<int>(type: "integer", nullable: false),
                    InventoryZoneId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScannedQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scans_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Scans_InventoryPeriods_InventoryPeriodId",
                        column: x => x.InventoryPeriodId,
                        principalTable: "InventoryPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Scans_InventoryZones_InventoryZoneId",
                        column: x => x.InventoryZoneId,
                        principalTable: "InventoryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Scans_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Scans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_AssetId",
                table: "Accessories",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCodes_AssetId",
                table: "AssetCodes",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCodes_CodeType_CodeValue",
                table: "AssetCodes",
                columns: new[] { "CodeType", "CodeValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetTypeId",
                table: "Assets",
                column: "AssetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_InventoryZoneId",
                table: "Assets",
                column: "InventoryZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ResponsiblePersonId",
                table: "Assets",
                column: "ResponsiblePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_SourceId",
                table: "Assets",
                column: "SourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_Name",
                table: "AssetTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPeriods_InventoryZoneId",
                table: "InventoryPeriods",
                column: "InventoryZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryZones_Code",
                table: "InventoryZones",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_InventoryZoneId",
                table: "Rooms",
                column: "InventoryZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_AssetId",
                table: "Scans",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_InventoryPeriodId",
                table: "Scans",
                column: "InventoryPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_InventoryZoneId",
                table: "Scans",
                column: "InventoryZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_RoomId",
                table: "Scans",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_UserId",
                table: "Scans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ResponsiblePersonId",
                table: "Users",
                column: "ResponsiblePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accessories");

            migrationBuilder.DropTable(
                name: "AssetCodes");

            migrationBuilder.DropTable(
                name: "Scans");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "InventoryPeriods");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AssetTypes");

            migrationBuilder.DropTable(
                name: "InventoryZones");

            migrationBuilder.DropTable(
                name: "ResponsiblePersons");
        }
    }
}
