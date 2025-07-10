using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MapApp.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialPins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Pins",
                columns: new[] { "Id", "Description", "ImageUrls", "Lat", "Lng" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7890-1234-567890abcdef"), "Karachi Port - A major seaport in Pakistan.", "[\"/images/default_port.jpg\"]", 24.860700000000001, 67.001099999999994 },
                    { new Guid("b2c3d4e5-f6a7-8901-2345-67890abcdef0"), "Frere Hall - Historic building in Karachi.", "[\"/images/default_frerehall.jpg\"]", 24.8567, 67.029700000000005 },
                    { new Guid("c3d4e5f6-a7b8-9012-3456-7890abcdef12"), "Clifton Beach - Popular recreational spot.", "[\"/images/default_cliftonbeach.jpg\"]", 24.779599999999999, 67.027799999999999 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Pins",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-1234-567890abcdef"));

            migrationBuilder.DeleteData(
                table: "Pins",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-8901-2345-67890abcdef0"));

            migrationBuilder.DeleteData(
                table: "Pins",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-9012-3456-7890abcdef12"));
        }
    }
}
