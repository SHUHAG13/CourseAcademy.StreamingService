using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseAcademy.Streaming.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Media");

            migrationBuilder.EnsureSchema(
                name: "Processing");

            migrationBuilder.EnsureSchema(
                name: "Messaging");

            migrationBuilder.CreateTable(
                name: "Asset",
                schema: "Media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KindCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalRef = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    RawObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    ThumbnailObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasCustomThumbnail = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastSequence = table.Column<long>(type: "bigint", nullable: false),
                    ReadyAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    DeleteRequestedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ArchiveExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asset", x => x.Id);
                    table.CheckConstraint("CK_Asset_Kind", "[KindCode] IN ('video', 'document')");
                    table.CheckConstraint("CK_Asset_Numbers", "[SizeBytes] > 0 AND [LastSequence] >= 0 AND ([DurationSeconds] IS NULL OR [DurationSeconds] >= 0) AND ([Width] IS NULL OR [Width] > 0) AND ([Height] IS NULL OR [Height] > 0)");
                    table.CheckConstraint("CK_Asset_Status", "[StatusCode] IN ('pending_upload', 'uploaded', 'queued', 'processing', 'ready', 'failed', 'deleting', 'deleted')");
                });

            migrationBuilder.CreateTable(
                name: "AssetTrack",
                schema: "Media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KindCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StreamIndex = table.Column<int>(type: "int", nullable: true),
                    LanguageCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SourceObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlaylistPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTrack", x => x.Id);
                    table.CheckConstraint("CK_AssetTrack_Kind", "[KindCode] IN ('audio', 'subtitle')");
                    table.CheckConstraint("CK_AssetTrack_Origin", "([SourceCode] = 'embedded' AND [StreamIndex] IS NOT NULL) OR ([SourceCode] = 'external' AND [KindCode] = 'subtitle' AND [SourceObjectKey] IS NOT NULL)");
                    table.CheckConstraint("CK_AssetTrack_Source", "[SourceCode] IN ('embedded', 'external')");
                    table.CheckConstraint("CK_AssetTrack_Status", "[StatusCode] IN ('pending', 'ready', 'failed')");
                    table.ForeignKey(
                        name: "FK_AssetTrack_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Media",
                        principalTable: "Asset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvent",
                schema: "Messaging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    GaveUpAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvent", x => x.Id);
                    table.CheckConstraint("CK_OutboxEvent_Numbers", "[Sequence] >= 1 AND [Attempts] >= 0");
                    table.ForeignKey(
                        name: "FK_OutboxEvent_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Media",
                        principalTable: "Asset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Rendition",
                schema: "Media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    BitrateKbps = table.Column<int>(type: "int", nullable: false),
                    Codec = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PlaylistPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rendition", x => x.Id);
                    table.CheckConstraint("CK_Rendition_Numbers", "[Width] > 0 AND [Height] > 0 AND [BitrateKbps] > 0");
                    table.ForeignKey(
                        name: "FK_Rendition_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Media",
                        principalTable: "Asset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TranscodeJob",
                schema: "Processing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LeaseOwner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Stage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    ProgressAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Engine = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscodeJob", x => x.Id);
                    table.CheckConstraint("CK_TranscodeJob_Numbers", "[Attempt] >= 0 AND [MaxAttempts] >= 1 AND [ProgressPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_TranscodeJob_Status", "[StatusCode] IN ('queued', 'running', 'succeeded', 'failed', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_TranscodeJob_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Media",
                        principalTable: "Asset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UploadSession",
                schema: "Media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    MultipartUploadId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    PartSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    PartCount = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    AbortedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSession", x => x.Id);
                    table.CheckConstraint("CK_UploadSession_Parts", "[PartSizeBytes] > 0 AND [PartCount] BETWEEN 1 AND 10000");
                    table.ForeignKey(
                        name: "FK_UploadSession_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Media",
                        principalTable: "Asset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobChunk",
                schema: "Processing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TranscodeJobId = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    StartSeconds = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    LengthSeconds = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobChunk", x => x.Id);
                    table.CheckConstraint("CK_JobChunk_Numbers", "[Index] >= 0 AND [StartSeconds] >= 0 AND [LengthSeconds] > 0");
                    table.CheckConstraint("CK_JobChunk_Status", "[StatusCode] IN ('pending', 'done')");
                    table.ForeignKey(
                        name: "FK_JobChunk_TranscodeJob_TranscodeJobId",
                        column: x => x.TranscodeJobId,
                        principalSchema: "Processing",
                        principalTable: "TranscodeJob",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ArchiveExpiresAt",
                schema: "Media",
                table: "Asset",
                column: "ArchiveExpiresAt",
                filter: "[ArchiveExpiresAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_PublicId",
                schema: "Media",
                table: "Asset",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asset_StatusCode",
                schema: "Media",
                table: "Asset",
                column: "StatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTrack_AssetId",
                schema: "Media",
                table: "AssetTrack",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTrack_PublicId",
                schema: "Media",
                table: "AssetTrack",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobChunk_TranscodeJobId_Index",
                schema: "Processing",
                table: "JobChunk",
                columns: new[] { "TranscodeJobId", "Index" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_AssetId_Sequence",
                schema: "Messaging",
                table: "OutboxEvent",
                columns: new[] { "AssetId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_EventId",
                schema: "Messaging",
                table: "OutboxEvent",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_NextAttemptAt",
                schema: "Messaging",
                table: "OutboxEvent",
                column: "NextAttemptAt",
                filter: "[DeliveredAt] IS NULL AND [GaveUpAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Rendition_AssetId_Height",
                schema: "Media",
                table: "Rendition",
                columns: new[] { "AssetId", "Height" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TranscodeJob_AssetId",
                schema: "Processing",
                table: "TranscodeJob",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscodeJob_StatusCode_LeaseExpiresAt",
                schema: "Processing",
                table: "TranscodeJob",
                columns: new[] { "StatusCode", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TranscodeJob_StatusCode_NextAttemptAt",
                schema: "Processing",
                table: "TranscodeJob",
                columns: new[] { "StatusCode", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadSession_AssetId",
                schema: "Media",
                table: "UploadSession",
                column: "AssetId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSession_ExpiresAt",
                schema: "Media",
                table: "UploadSession",
                column: "ExpiresAt",
                filter: "[CompletedAt] IS NULL AND [AbortedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetTrack",
                schema: "Media");

            migrationBuilder.DropTable(
                name: "JobChunk",
                schema: "Processing");

            migrationBuilder.DropTable(
                name: "OutboxEvent",
                schema: "Messaging");

            migrationBuilder.DropTable(
                name: "Rendition",
                schema: "Media");

            migrationBuilder.DropTable(
                name: "UploadSession",
                schema: "Media");

            migrationBuilder.DropTable(
                name: "TranscodeJob",
                schema: "Processing");

            migrationBuilder.DropTable(
                name: "Asset",
                schema: "Media");
        }
    }
}
