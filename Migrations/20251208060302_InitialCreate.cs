using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BaseballApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblBatter",
                columns: table => new
                {
                    playerId = table.Column<string>(type: "text", nullable: false),
                    playerName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblBatter", x => x.playerId);
                });

            migrationBuilder.CreateTable(
                name: "tblBattingRankingCache",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    seasonId = table.Column<string>(type: "text", nullable: false),
                    playerId = table.Column<string>(type: "text", nullable: false),
                    playerName = table.Column<string>(type: "text", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    games = table.Column<int>(type: "integer", nullable: false),
                    pa = table.Column<int>(type: "integer", nullable: false),
                    ab = table.Column<int>(type: "integer", nullable: false),
                    h = table.Column<int>(type: "integer", nullable: false),
                    twoB = table.Column<int>(type: "integer", nullable: false),
                    threeB = table.Column<int>(type: "integer", nullable: false),
                    hr = table.Column<int>(type: "integer", nullable: false),
                    rbi = table.Column<int>(type: "integer", nullable: false),
                    r = table.Column<int>(type: "integer", nullable: false),
                    so = table.Column<int>(type: "integer", nullable: false),
                    bb = table.Column<int>(type: "integer", nullable: false),
                    HBP = table.Column<int>(type: "integer", nullable: false),
                    SF = table.Column<int>(type: "integer", nullable: false),
                    sb = table.Column<int>(type: "integer", nullable: false),
                    avg = table.Column<decimal>(type: "numeric(5,3)", nullable: false),
                    obp = table.Column<decimal>(type: "numeric(5,3)", nullable: false),
                    slg = table.Column<decimal>(type: "numeric(5,3)", nullable: false),
                    ops = table.Column<decimal>(type: "numeric(5,3)", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblBattingRankingCache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tblPitcher",
                columns: table => new
                {
                    playerId = table.Column<string>(type: "text", nullable: false),
                    playerName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPitcher", x => x.playerId);
                });

            migrationBuilder.CreateTable(
                name: "tblPitchingRankingCache",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    seasonId = table.Column<string>(type: "text", nullable: false),
                    playerId = table.Column<string>(type: "text", nullable: false),
                    playerName = table.Column<string>(type: "text", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    games = table.Column<int>(type: "integer", nullable: false),
                    ip = table.Column<decimal>(type: "numeric(5,1)", nullable: false),
                    ipOuts = table.Column<int>(type: "integer", nullable: false),
                    h = table.Column<int>(type: "integer", nullable: false),
                    hr = table.Column<int>(type: "integer", nullable: false),
                    bb = table.Column<int>(type: "integer", nullable: false),
                    so = table.Column<int>(type: "integer", nullable: false),
                    r = table.Column<int>(type: "integer", nullable: false),
                    er = table.Column<int>(type: "integer", nullable: false),
                    w = table.Column<int>(type: "integer", nullable: false),
                    l = table.Column<int>(type: "integer", nullable: false),
                    era = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    whip = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    k9 = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    bb9 = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    kbbRatio = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    baa = table.Column<decimal>(type: "numeric(5,3)", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPitchingRankingCache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tblSeason",
                columns: table => new
                {
                    seasonId = table.Column<string>(type: "text", nullable: false),
                    season = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSeason", x => x.seasonId);
                });

            migrationBuilder.CreateTable(
                name: "tblStadium",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stadium = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblStadium", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tblTeam",
                columns: table => new
                {
                    teamId = table.Column<string>(type: "text", nullable: false),
                    teamName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTeam", x => x.teamId);
                });

            migrationBuilder.CreateTable(
                name: "tblGame",
                columns: table => new
                {
                    seasonId = table.Column<string>(type: "text", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    stadiumId = table.Column<int>(type: "integer", nullable: true),
                    awayTeamId = table.Column<string>(type: "text", nullable: true),
                    homeTeamId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblGame", x => new { x.seasonId, x.seq });
                    table.ForeignKey(
                        name: "FK_tblGame_tblSeason_seasonId",
                        column: x => x.seasonId,
                        principalTable: "tblSeason",
                        principalColumn: "seasonId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblGame_tblStadium_stadiumId",
                        column: x => x.stadiumId,
                        principalTable: "tblStadium",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblGame_tblTeam_awayTeamId",
                        column: x => x.awayTeamId,
                        principalTable: "tblTeam",
                        principalColumn: "teamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblGame_tblTeam_homeTeamId",
                        column: x => x.homeTeamId,
                        principalTable: "tblTeam",
                        principalColumn: "teamId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblPlayerTeam",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    playerId = table.Column<string>(type: "text", nullable: false),
                    teamId = table.Column<string>(type: "text", nullable: false),
                    seasonId = table.Column<string>(type: "text", nullable: false),
                    playerNumber = table.Column<string>(type: "text", nullable: false),
                    startDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    endDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPlayerTeam", x => x.id);
                    table.ForeignKey(
                        name: "FK_tblPlayerTeam_tblBatter_playerId",
                        column: x => x.playerId,
                        principalTable: "tblBatter",
                        principalColumn: "playerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblPlayerTeam_tblPitcher_playerId",
                        column: x => x.playerId,
                        principalTable: "tblPitcher",
                        principalColumn: "playerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblPlayerTeam_tblSeason_seasonId",
                        column: x => x.seasonId,
                        principalTable: "tblSeason",
                        principalColumn: "seasonId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblPlayerTeam_tblTeam_teamId",
                        column: x => x.teamId,
                        principalTable: "tblTeam",
                        principalColumn: "teamId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblBatterBox",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    seasonId = table.Column<string>(type: "text", nullable: false),
                    gameSeq = table.Column<int>(type: "integer", nullable: false),
                    homeOrAway = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    subOrder = table.Column<int>(type: "integer", nullable: false),
                    playerId = table.Column<string>(type: "text", nullable: false),
                    PA = table.Column<int>(type: "integer", nullable: false),
                    AB = table.Column<int>(type: "integer", nullable: false),
                    R = table.Column<int>(type: "integer", nullable: false),
                    H = table.Column<int>(type: "integer", nullable: false),
                    RBI = table.Column<int>(type: "integer", nullable: false),
                    _2B = table.Column<int>(name: "2B", type: "integer", nullable: false),
                    _3B = table.Column<int>(name: "3B", type: "integer", nullable: false),
                    HR = table.Column<int>(type: "integer", nullable: false),
                    GIDP = table.Column<int>(type: "integer", nullable: false),
                    DP = table.Column<int>(type: "integer", nullable: false),
                    TP = table.Column<int>(type: "integer", nullable: false),
                    BB = table.Column<int>(type: "integer", nullable: false),
                    IBB = table.Column<int>(type: "integer", nullable: false),
                    HBP = table.Column<int>(type: "integer", nullable: false),
                    SO = table.Column<int>(type: "integer", nullable: false),
                    SH = table.Column<int>(type: "integer", nullable: false),
                    SF = table.Column<int>(type: "integer", nullable: false),
                    E = table.Column<int>(type: "integer", nullable: false),
                    SB = table.Column<int>(type: "integer", nullable: false),
                    CS = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblBatterBox", x => x.id);
                    table.ForeignKey(
                        name: "FK_tblBatterBox_tblBatter_playerId",
                        column: x => x.playerId,
                        principalTable: "tblBatter",
                        principalColumn: "playerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblBatterBox_tblGame_seasonId_gameSeq",
                        columns: x => new { x.seasonId, x.gameSeq },
                        principalTable: "tblGame",
                        principalColumns: new[] { "seasonId", "seq" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblPA",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    seasonId = table.Column<string>(type: "text", nullable: false),
                    gameSeq = table.Column<int>(type: "integer", nullable: false),
                    homeOrAway = table.Column<string>(type: "text", nullable: false),
                    inning = table.Column<int>(type: "integer", nullable: false),
                    paSeq = table.Column<int>(type: "integer", nullable: false),
                    Scored = table.Column<bool>(type: "boolean", nullable: false),
                    batterId = table.Column<string>(type: "text", nullable: true),
                    BatterHand = table.Column<string>(type: "text", nullable: true),
                    pitcherId = table.Column<string>(type: "text", nullable: true),
                    PitcherHand = table.Column<string>(type: "text", nullable: true),
                    catcherId = table.Column<string>(type: "text", nullable: true),
                    PaRound = table.Column<int>(type: "integer", nullable: true),
                    PaOrder = table.Column<int>(type: "integer", nullable: true),
                    IsPH = table.Column<bool>(type: "boolean", nullable: false),
                    AwayScores = table.Column<int>(type: "integer", nullable: true),
                    HomeScores = table.Column<int>(type: "integer", nullable: true),
                    Strikes = table.Column<int>(type: "integer", nullable: true),
                    Balls = table.Column<int>(type: "integer", nullable: true),
                    Outs = table.Column<int>(type: "integer", nullable: true),
                    bases = table.Column<int>(type: "integer", nullable: true),
                    HomeWE = table.Column<decimal>(type: "numeric", nullable: true),
                    RE = table.Column<decimal>(type: "numeric", nullable: true),
                    Result = table.Column<string>(type: "text", nullable: true),
                    RBI = table.Column<int>(type: "integer", nullable: true),
                    LocationCode = table.Column<string>(type: "text", nullable: true),
                    Trajectory = table.Column<string>(type: "text", nullable: true),
                    Hardness = table.Column<string>(type: "text", nullable: true),
                    EndAwayScores = table.Column<int>(type: "integer", nullable: true),
                    EndHomeScores = table.Column<int>(type: "integer", nullable: true),
                    EndOuts = table.Column<int>(type: "integer", nullable: true),
                    endBases = table.Column<int>(type: "integer", nullable: true),
                    WPA = table.Column<decimal>(type: "numeric", nullable: true),
                    RE24 = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPA", x => x.id);
                    table.ForeignKey(
                        name: "FK_tblPA_tblBatter_batterId",
                        column: x => x.batterId,
                        principalTable: "tblBatter",
                        principalColumn: "playerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblPA_tblBatter_catcherId",
                        column: x => x.catcherId,
                        principalTable: "tblBatter",
                        principalColumn: "playerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblPA_tblGame_seasonId_gameSeq",
                        columns: x => new { x.seasonId, x.gameSeq },
                        principalTable: "tblGame",
                        principalColumns: new[] { "seasonId", "seq" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblPA_tblPitcher_pitcherId",
                        column: x => x.pitcherId,
                        principalTable: "tblPitcher",
                        principalColumn: "playerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblPitcherBox",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    seasonId = table.Column<string>(type: "text", nullable: false),
                    gameSeq = table.Column<int>(type: "integer", nullable: false),
                    homeOrAway = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    playerId = table.Column<string>(type: "text", nullable: true),
                    IPOuts = table.Column<int>(type: "integer", nullable: true),
                    NP = table.Column<int>(type: "integer", nullable: true),
                    BF = table.Column<int>(type: "integer", nullable: true),
                    H = table.Column<int>(type: "integer", nullable: true),
                    HR = table.Column<int>(type: "integer", nullable: true),
                    BB = table.Column<int>(type: "integer", nullable: true),
                    IBB = table.Column<int>(type: "integer", nullable: true),
                    HB = table.Column<int>(type: "integer", nullable: true),
                    SO = table.Column<int>(type: "integer", nullable: true),
                    R = table.Column<int>(type: "integer", nullable: true),
                    ER = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPitcherBox", x => x.id);
                    table.ForeignKey(
                        name: "FK_tblPitcherBox_tblGame_seasonId_gameSeq",
                        columns: x => new { x.seasonId, x.gameSeq },
                        principalTable: "tblGame",
                        principalColumns: new[] { "seasonId", "seq" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblPitcherBox_tblPitcher_playerId",
                        column: x => x.playerId,
                        principalTable: "tblPitcher",
                        principalColumn: "playerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblScores",
                columns: table => new
                {
                    seasonId = table.Column<string>(type: "text", nullable: false),
                    gameSeq = table.Column<int>(type: "integer", nullable: false),
                    homeOrAway = table.Column<string>(type: "text", nullable: false),
                    inning = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    GameSeasonId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblScores", x => x.seasonId);
                    table.ForeignKey(
                        name: "FK_tblScores_tblGame_GameSeasonId_gameSeq",
                        columns: x => new { x.GameSeasonId, x.gameSeq },
                        principalTable: "tblGame",
                        principalColumns: new[] { "seasonId", "seq" });
                });

            migrationBuilder.CreateTable(
                name: "tblEvent",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    InPlay = table.Column<bool>(type: "boolean", nullable: false),
                    IsStrike = table.Column<bool>(type: "boolean", nullable: false),
                    IsBall = table.Column<bool>(type: "boolean", nullable: false),
                    PitcherId = table.Column<string>(type: "text", nullable: true),
                    CatcherId = table.Column<string>(type: "text", nullable: true),
                    BatterId = table.Column<string>(type: "text", nullable: true),
                    pitchCode = table.Column<string>(type: "text", nullable: true),
                    pitchType = table.Column<string>(type: "text", nullable: true),
                    velocity = table.Column<decimal>(type: "numeric", nullable: true),
                    coordX = table.Column<decimal>(type: "numeric", nullable: true),
                    coordY = table.Column<decimal>(type: "numeric", nullable: true),
                    PAId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblEvent", x => x.id);
                    table.ForeignKey(
                        name: "FK_tblEvent_tblBatter_BatterId",
                        column: x => x.BatterId,
                        principalTable: "tblBatter",
                        principalColumn: "playerId");
                    table.ForeignKey(
                        name: "FK_tblEvent_tblBatter_CatcherId",
                        column: x => x.CatcherId,
                        principalTable: "tblBatter",
                        principalColumn: "playerId");
                    table.ForeignKey(
                        name: "FK_tblEvent_tblPA_PAId",
                        column: x => x.PAId,
                        principalTable: "tblPA",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tblEvent_tblPA_paId",
                        column: x => x.paId,
                        principalTable: "tblPA",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblEvent_tblPitcher_PitcherId",
                        column: x => x.PitcherId,
                        principalTable: "tblPitcher",
                        principalColumn: "playerId");
                });

            migrationBuilder.CreateTable(
                name: "tblRunner",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    runnerId = table.Column<string>(type: "text", nullable: false),
                    IsOut = table.Column<bool>(type: "boolean", nullable: false),
                    Scored = table.Column<bool>(type: "boolean", nullable: false),
                    IsRBI = table.Column<bool>(type: "boolean", nullable: false),
                    IsER = table.Column<bool>(type: "boolean", nullable: false),
                    ERPitcherId = table.Column<string>(type: "text", nullable: true),
                    RunnerPlayerPlayerId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblRunner", x => x.id);
                    table.ForeignKey(
                        name: "FK_tblRunner_tblBatter_RunnerPlayerPlayerId",
                        column: x => x.RunnerPlayerPlayerId,
                        principalTable: "tblBatter",
                        principalColumn: "playerId");
                    table.ForeignKey(
                        name: "FK_tblRunner_tblEvent_EventId",
                        column: x => x.EventId,
                        principalTable: "tblEvent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblRunner_tblPitcher_ERPitcherId",
                        column: x => x.ERPitcherId,
                        principalTable: "tblPitcher",
                        principalColumn: "playerId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblBatterBox_playerId",
                table: "tblBatterBox",
                column: "playerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblBatterBox_seasonId_gameSeq",
                table: "tblBatterBox",
                columns: new[] { "seasonId", "gameSeq" });

            migrationBuilder.CreateIndex(
                name: "IX_tblBattingRankingCache_seasonId_playerId",
                table: "tblBattingRankingCache",
                columns: new[] { "seasonId", "playerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblBattingRankingCache_seasonId_rank",
                table: "tblBattingRankingCache",
                columns: new[] { "seasonId", "rank" });

            migrationBuilder.CreateIndex(
                name: "IX_tblEvent_BatterId",
                table: "tblEvent",
                column: "BatterId");

            migrationBuilder.CreateIndex(
                name: "IX_tblEvent_CatcherId",
                table: "tblEvent",
                column: "CatcherId");

            migrationBuilder.CreateIndex(
                name: "IX_tblEvent_paId",
                table: "tblEvent",
                column: "paId");

            migrationBuilder.CreateIndex(
                name: "IX_tblEvent_PAId",
                table: "tblEvent",
                column: "PAId");

            migrationBuilder.CreateIndex(
                name: "IX_tblEvent_PitcherId",
                table: "tblEvent",
                column: "PitcherId");

            migrationBuilder.CreateIndex(
                name: "IX_tblGame_awayTeamId",
                table: "tblGame",
                column: "awayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_tblGame_homeTeamId",
                table: "tblGame",
                column: "homeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_tblGame_stadiumId",
                table: "tblGame",
                column: "stadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPA_batterId",
                table: "tblPA",
                column: "batterId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPA_catcherId",
                table: "tblPA",
                column: "catcherId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPA_pitcherId",
                table: "tblPA",
                column: "pitcherId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPA_seasonId_gameSeq",
                table: "tblPA",
                columns: new[] { "seasonId", "gameSeq" });

            migrationBuilder.CreateIndex(
                name: "IX_tblPitcherBox_playerId",
                table: "tblPitcherBox",
                column: "playerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPitcherBox_seasonId_gameSeq",
                table: "tblPitcherBox",
                columns: new[] { "seasonId", "gameSeq" });

            migrationBuilder.CreateIndex(
                name: "IX_tblPitchingRankingCache_seasonId_playerId",
                table: "tblPitchingRankingCache",
                columns: new[] { "seasonId", "playerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblPitchingRankingCache_seasonId_rank",
                table: "tblPitchingRankingCache",
                columns: new[] { "seasonId", "rank" });

            migrationBuilder.CreateIndex(
                name: "IX_tblPlayerTeam_playerId",
                table: "tblPlayerTeam",
                column: "playerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPlayerTeam_seasonId",
                table: "tblPlayerTeam",
                column: "seasonId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPlayerTeam_teamId",
                table: "tblPlayerTeam",
                column: "teamId");

            migrationBuilder.CreateIndex(
                name: "IX_tblRunner_ERPitcherId",
                table: "tblRunner",
                column: "ERPitcherId");

            migrationBuilder.CreateIndex(
                name: "IX_tblRunner_EventId",
                table: "tblRunner",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_tblRunner_RunnerPlayerPlayerId",
                table: "tblRunner",
                column: "RunnerPlayerPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblScores_GameSeasonId_gameSeq",
                table: "tblScores",
                columns: new[] { "GameSeasonId", "gameSeq" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblBatterBox");

            migrationBuilder.DropTable(
                name: "tblBattingRankingCache");

            migrationBuilder.DropTable(
                name: "tblPitcherBox");

            migrationBuilder.DropTable(
                name: "tblPitchingRankingCache");

            migrationBuilder.DropTable(
                name: "tblPlayerTeam");

            migrationBuilder.DropTable(
                name: "tblRunner");

            migrationBuilder.DropTable(
                name: "tblScores");

            migrationBuilder.DropTable(
                name: "tblEvent");

            migrationBuilder.DropTable(
                name: "tblPA");

            migrationBuilder.DropTable(
                name: "tblBatter");

            migrationBuilder.DropTable(
                name: "tblGame");

            migrationBuilder.DropTable(
                name: "tblPitcher");

            migrationBuilder.DropTable(
                name: "tblSeason");

            migrationBuilder.DropTable(
                name: "tblStadium");

            migrationBuilder.DropTable(
                name: "tblTeam");
        }
    }
}
