using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MapApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Lat = table.Column<double>(type: "double precision", nullable: false),
                    Lng = table.Column<double>(type: "double precision", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ImageUrls = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pins", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Pins",
                columns: new[] { "Id", "Description", "ImageUrls", "Lat", "Lng" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7890-1234-567890abcdef"), "Karachi Port - A major seaport in Pakistan.", new List<string> { "/images/default_port.jpg" }, 24.860700000000001, 67.001099999999994 },
                    { new Guid("b2c3d4e5-f6a7-8901-2345-67890abcdef0"), "Frere Hall - Historic building in Karachi.", new List<string> { "/images/default_frerehall.jpg" }, 24.8567, 67.029700000000005 },
                    { new Guid("c3d4e5f6-a7b8-9012-3456-7890abcdef12"), "Clifton Beach - Popular recreational spot.", new List<string> { "/images/default_cliftonbeach.jpg" }, 24.779599999999999, 67.027799999999999 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pins");
        }
    }
}
