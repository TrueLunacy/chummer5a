/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */
﻿using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace ChummerHub.Migrations
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'favgroupsAreGuids'
    public partial class favgroupsAreGuids : Migration
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'favgroupsAreGuids'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'favgroupsAreGuids.Up(MigrationBuilder)'
        protected override void Up(MigrationBuilder migrationBuilder)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'favgroupsAreGuids.Up(MigrationBuilder)'
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SINnerGroups_AspNetUsers_ApplicationUserId",
                table: "SINnerGroups");

            migrationBuilder.DropIndex(
                name: "IX_SINnerGroups_ApplicationUserId",
                table: "SINnerGroups");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "SINnerGroups");

            migrationBuilder.CreateTable(
                name: "ApplicationUserFavoriteGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    FavoriteGuid = table.Column<Guid>(nullable: false),
                    ApplicationUserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserFavoriteGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationUserFavoriteGroup_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserFavoriteGroup_ApplicationUserId",
                table: "ApplicationUserFavoriteGroup",
                column: "ApplicationUserId");
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'favgroupsAreGuids.Down(MigrationBuilder)'
        protected override void Down(MigrationBuilder migrationBuilder)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'favgroupsAreGuids.Down(MigrationBuilder)'
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserFavoriteGroup");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId",
                table: "SINnerGroups",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SINnerGroups_ApplicationUserId",
                table: "SINnerGroups",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SINnerGroups_AspNetUsers_ApplicationUserId",
                table: "SINnerGroups",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
