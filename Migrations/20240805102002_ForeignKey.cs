using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsAggregation.Migrations
{
    /// <inheritdoc />
    public partial class ForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_ArticleId",
                table: "WatchHistories",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_UserId",
                table: "WatchHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReports_CommentId",
                table: "CommentReports",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReports_UserId",
                table: "CommentReports",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentReports_Comments_CommentId",
                table: "CommentReports",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentReports_Users_UserId",
                table: "CommentReports",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchHistories_Articles_ArticleId",
                table: "WatchHistories",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchHistories_Users_UserId",
                table: "WatchHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentReports_Comments_CommentId",
                table: "CommentReports");

            migrationBuilder.DropForeignKey(
                name: "FK_CommentReports_Users_UserId",
                table: "CommentReports");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchHistories_Articles_ArticleId",
                table: "WatchHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchHistories_Users_UserId",
                table: "WatchHistories");

            migrationBuilder.DropIndex(
                name: "IX_WatchHistories_ArticleId",
                table: "WatchHistories");

            migrationBuilder.DropIndex(
                name: "IX_WatchHistories_UserId",
                table: "WatchHistories");

            migrationBuilder.DropIndex(
                name: "IX_CommentReports_CommentId",
                table: "CommentReports");

            migrationBuilder.DropIndex(
                name: "IX_CommentReports_UserId",
                table: "CommentReports");
        }
    }
}
