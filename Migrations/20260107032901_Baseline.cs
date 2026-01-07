using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseballApp.Migrations
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblBatter",
                columns: table => new
                {
                    playerId = table.Column<string>(type: "TEXT", nullable: false),
                    playerName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblBatter", x => x.playerId);
                });

            migrationBuilder.CreateTable(
                name: "tblBattingRankingCache",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    playerId = table.Column<string>(type: "TEXT", nullable: false),
                    playerName = table.Column<string>(type: "TEXT", nullable: false),
                    rank = table.Column<int>(type: "INTEGER", nullable: false),
                    games = table.Column<int>(type: "INTEGER", nullable: false),
                    pa = table.Column<int>(type: "INTEGER", nullable: false),
                    ab = table.Column<int>(type: "INTEGER", nullable: false),
                    h = table.Column<int>(type: "INTEGER", nullable: false),
                    twoB = table.Column<int>(type: "INTEGER", nullable: false),
                    threeB = table.Column<int>(type: "INTEGER", nullable: false),
                    hr = table.Column<int>(type: "INTEGER", nullable: false),
                    rbi = table.Column<int>(type: "INTEGER", nullable: false),
                    r = table.Column<int>(type: "INTEGER", nullable: false),
                    so = table.Column<int>(type: "INTEGER", nullable: false),
                    bb = table.Column<int>(type: "INTEGER", nullable: false),
                    HBP = table.Column<int>(type: "INTEGER", nullable: false),
                    SF = table.Column<int>(type: "INTEGER", nullable: false),
                    sb = table.Column<int>(type: "INTEGER", nullable: false),
                    avg = table.Column<decimal>(type: "decimal(5,3)", nullable: false),
                    obp = table.Column<decimal>(type: "decimal(5,3)", nullable: false),
                    slg = table.Column<decimal>(type: "decimal(5,3)", nullable: false),
                    ops = table.Column<decimal>(type: "decimal(5,3)", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblBattingRankingCache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tblCodeBases",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCodeBases", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tblCodeEventType",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCodeEventType", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tblCodeHardness",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCodeHardness", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tblCodePitchCode",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCodePitchCode", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tblCodePitchType",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCodePitchType", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tblCodeResult",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCodeResult", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tblCodeRunnerType",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCodeRunnerType", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tblCodeTrajectory",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCodeTrajectory", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tblPitcher",
                columns: table => new
                {
                    playerId = table.Column<string>(type: "TEXT", nullable: false),
                    playerName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPitcher", x => x.playerId);
                });

            migrationBuilder.CreateTable(
                name: "tblPitchingRankingCache",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    playerId = table.Column<string>(type: "TEXT", nullable: false),
                    playerName = table.Column<string>(type: "TEXT", nullable: false),
                    rank = table.Column<int>(type: "INTEGER", nullable: false),
                    games = table.Column<int>(type: "INTEGER", nullable: false),
                    ip = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    ipOuts = table.Column<int>(type: "INTEGER", nullable: false),
                    h = table.Column<int>(type: "INTEGER", nullable: false),
                    hr = table.Column<int>(type: "INTEGER", nullable: false),
                    bb = table.Column<int>(type: "INTEGER", nullable: false),
                    so = table.Column<int>(type: "INTEGER", nullable: false),
                    r = table.Column<int>(type: "INTEGER", nullable: false),
                    er = table.Column<int>(type: "INTEGER", nullable: false),
                    w = table.Column<int>(type: "INTEGER", nullable: false),
                    l = table.Column<int>(type: "INTEGER", nullable: false),
                    era = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    whip = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    k9 = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    bb9 = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    kbbRatio = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    baa = table.Column<decimal>(type: "decimal(5,3)", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPitchingRankingCache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tblSeason",
                columns: table => new
                {
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    season = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSeason", x => x.seasonId);
                });

            migrationBuilder.CreateTable(
                name: "tblStadium",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    stadium = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblStadium", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tblTeam",
                columns: table => new
                {
                    teamId = table.Column<string>(type: "TEXT", nullable: false),
                    teamName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTeam", x => x.teamId);
                });

            migrationBuilder.CreateTable(
                name: "tblTeamGameStats",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    gameId = table.Column<string>(type: "TEXT", nullable: false),
                    gameDate = table.Column<string>(type: "TEXT", nullable: false),
                    teamId = table.Column<string>(type: "TEXT", nullable: false),
                    teamName = table.Column<string>(type: "TEXT", nullable: false),
                    opponentTeamId = table.Column<string>(type: "TEXT", nullable: false),
                    opponentTeamName = table.Column<string>(type: "TEXT", nullable: false),
                    isHome = table.Column<int>(type: "INTEGER", nullable: false),
                    teamScore = table.Column<int>(type: "INTEGER", nullable: false),
                    opponentScore = table.Column<int>(type: "INTEGER", nullable: false),
                    pa = table.Column<int>(type: "INTEGER", nullable: false),
                    ab = table.Column<int>(type: "INTEGER", nullable: false),
                    h = table.Column<int>(type: "INTEGER", nullable: false),
                    twoB = table.Column<int>(type: "INTEGER", nullable: false),
                    threeB = table.Column<int>(type: "INTEGER", nullable: false),
                    hr = table.Column<int>(type: "INTEGER", nullable: false),
                    bb = table.Column<int>(type: "INTEGER", nullable: false),
                    so = table.Column<int>(type: "INTEGER", nullable: false),
                    hbp = table.Column<int>(type: "INTEGER", nullable: false),
                    sf = table.Column<int>(type: "INTEGER", nullable: false),
                    sb = table.Column<int>(type: "INTEGER", nullable: false),
                    cs = table.Column<int>(type: "INTEGER", nullable: false),
                    ipOuts = table.Column<int>(type: "INTEGER", nullable: false),
                    er = table.Column<int>(type: "INTEGER", nullable: false),
                    hitsAllowed = table.Column<int>(type: "INTEGER", nullable: false),
                    bbAllowed = table.Column<int>(type: "INTEGER", nullable: false),
                    soPitching = table.Column<int>(type: "INTEGER", nullable: false),
                    hrAllowed = table.Column<int>(type: "INTEGER", nullable: false),
                    createdAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTeamGameStats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tblTeamSeasonRankingCache",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    teamId = table.Column<string>(type: "TEXT", nullable: false),
                    teamName = table.Column<string>(type: "TEXT", nullable: false),
                    rank = table.Column<int>(type: "INTEGER", nullable: false),
                    gamesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    wins = table.Column<int>(type: "INTEGER", nullable: false),
                    losses = table.Column<int>(type: "INTEGER", nullable: false),
                    runsScored = table.Column<int>(type: "INTEGER", nullable: false),
                    runsAllowed = table.Column<int>(type: "INTEGER", nullable: false),
                    pa = table.Column<int>(type: "INTEGER", nullable: false),
                    ab = table.Column<int>(type: "INTEGER", nullable: false),
                    h = table.Column<int>(type: "INTEGER", nullable: false),
                    twoB = table.Column<int>(type: "INTEGER", nullable: false),
                    threeB = table.Column<int>(type: "INTEGER", nullable: false),
                    hr = table.Column<int>(type: "INTEGER", nullable: false),
                    bb = table.Column<int>(type: "INTEGER", nullable: false),
                    so = table.Column<int>(type: "INTEGER", nullable: false),
                    hbp = table.Column<int>(type: "INTEGER", nullable: false),
                    sf = table.Column<int>(type: "INTEGER", nullable: false),
                    sb = table.Column<int>(type: "INTEGER", nullable: false),
                    cs = table.Column<int>(type: "INTEGER", nullable: false),
                    avg = table.Column<decimal>(type: "decimal(5,3)", nullable: false),
                    obp = table.Column<decimal>(type: "decimal(5,3)", nullable: false),
                    slg = table.Column<decimal>(type: "decimal(5,3)", nullable: false),
                    ops = table.Column<decimal>(type: "decimal(5,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTeamSeasonRankingCache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tblGame",
                columns: table => new
                {
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    seq = table.Column<int>(type: "INTEGER", nullable: false),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    stadiumId = table.Column<int>(type: "INTEGER", nullable: true),
                    awayTeamId = table.Column<string>(type: "TEXT", nullable: true),
                    homeTeamId = table.Column<string>(type: "TEXT", nullable: true)
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
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    playerId = table.Column<string>(type: "TEXT", nullable: false),
                    teamId = table.Column<string>(type: "TEXT", nullable: false),
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    playerNumber = table.Column<string>(type: "TEXT", nullable: false),
                    startDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    endDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    isActive = table.Column<bool>(type: "INTEGER", nullable: false)
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
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    gameSeq = table.Column<int>(type: "INTEGER", nullable: false),
                    homeOrAway = table.Column<string>(type: "TEXT", nullable: false),
                    order = table.Column<int>(type: "INTEGER", nullable: false),
                    subOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    playerId = table.Column<string>(type: "TEXT", nullable: false),
                    PA = table.Column<int>(type: "INTEGER", nullable: false),
                    AB = table.Column<int>(type: "INTEGER", nullable: false),
                    R = table.Column<int>(type: "INTEGER", nullable: false),
                    H = table.Column<int>(type: "INTEGER", nullable: false),
                    RBI = table.Column<int>(type: "INTEGER", nullable: false),
                    _2B = table.Column<int>(name: "2B", type: "INTEGER", nullable: false),
                    _3B = table.Column<int>(name: "3B", type: "INTEGER", nullable: false),
                    HR = table.Column<int>(type: "INTEGER", nullable: false),
                    GIDP = table.Column<int>(type: "INTEGER", nullable: false),
                    DP = table.Column<int>(type: "INTEGER", nullable: false),
                    TP = table.Column<int>(type: "INTEGER", nullable: false),
                    BB = table.Column<int>(type: "INTEGER", nullable: false),
                    IBB = table.Column<int>(type: "INTEGER", nullable: false),
                    HBP = table.Column<int>(type: "INTEGER", nullable: false),
                    SO = table.Column<int>(type: "INTEGER", nullable: false),
                    SH = table.Column<int>(type: "INTEGER", nullable: false),
                    SF = table.Column<int>(type: "INTEGER", nullable: false),
                    E = table.Column<int>(type: "INTEGER", nullable: false),
                    SB = table.Column<int>(type: "INTEGER", nullable: false),
                    CS = table.Column<int>(type: "INTEGER", nullable: false)
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
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    gameSeq = table.Column<int>(type: "INTEGER", nullable: false),
                    homeOrAway = table.Column<string>(type: "TEXT", nullable: false),
                    inning = table.Column<int>(type: "INTEGER", nullable: false),
                    paSeq = table.Column<int>(type: "INTEGER", nullable: false),
                    Scored = table.Column<bool>(type: "INTEGER", nullable: false),
                    batterId = table.Column<string>(type: "TEXT", nullable: true),
                    BatterHand = table.Column<string>(type: "TEXT", nullable: true),
                    pitcherId = table.Column<string>(type: "TEXT", nullable: true),
                    PitcherHand = table.Column<string>(type: "TEXT", nullable: true),
                    catcherId = table.Column<string>(type: "TEXT", nullable: true),
                    PaRound = table.Column<int>(type: "INTEGER", nullable: true),
                    PaOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    IsPH = table.Column<bool>(type: "INTEGER", nullable: false),
                    AwayScores = table.Column<int>(type: "INTEGER", nullable: true),
                    HomeScores = table.Column<int>(type: "INTEGER", nullable: true),
                    Strikes = table.Column<int>(type: "INTEGER", nullable: true),
                    Balls = table.Column<int>(type: "INTEGER", nullable: true),
                    Outs = table.Column<int>(type: "INTEGER", nullable: true),
                    bases = table.Column<int>(type: "INTEGER", nullable: true),
                    HomeWE = table.Column<decimal>(type: "TEXT", nullable: true),
                    RE = table.Column<decimal>(type: "TEXT", nullable: true),
                    Result = table.Column<string>(type: "TEXT", nullable: true),
                    RBI = table.Column<int>(type: "INTEGER", nullable: true),
                    LocationCode = table.Column<string>(type: "TEXT", nullable: true),
                    Trajectory = table.Column<string>(type: "TEXT", nullable: true),
                    Hardness = table.Column<string>(type: "TEXT", nullable: true),
                    EndAwayScores = table.Column<int>(type: "INTEGER", nullable: true),
                    EndHomeScores = table.Column<int>(type: "INTEGER", nullable: true),
                    EndOuts = table.Column<int>(type: "INTEGER", nullable: true),
                    endBases = table.Column<int>(type: "INTEGER", nullable: true),
                    WPA = table.Column<decimal>(type: "TEXT", nullable: true),
                    RE24 = table.Column<decimal>(type: "TEXT", nullable: true)
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
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    gameSeq = table.Column<int>(type: "INTEGER", nullable: false),
                    homeOrAway = table.Column<string>(type: "TEXT", nullable: false),
                    order = table.Column<int>(type: "INTEGER", nullable: false),
                    playerId = table.Column<string>(type: "TEXT", nullable: true),
                    IPOuts = table.Column<int>(type: "INTEGER", nullable: true),
                    NP = table.Column<int>(type: "INTEGER", nullable: true),
                    BF = table.Column<int>(type: "INTEGER", nullable: true),
                    H = table.Column<int>(type: "INTEGER", nullable: true),
                    HR = table.Column<int>(type: "INTEGER", nullable: true),
                    BB = table.Column<int>(type: "INTEGER", nullable: true),
                    IBB = table.Column<int>(type: "INTEGER", nullable: true),
                    HB = table.Column<int>(type: "INTEGER", nullable: true),
                    SO = table.Column<int>(type: "INTEGER", nullable: true),
                    R = table.Column<int>(type: "INTEGER", nullable: true),
                    ER = table.Column<int>(type: "INTEGER", nullable: true)
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
                    seasonId = table.Column<string>(type: "TEXT", nullable: false),
                    gameSeq = table.Column<int>(type: "INTEGER", nullable: false),
                    homeOrAway = table.Column<string>(type: "TEXT", nullable: false),
                    inning = table.Column<int>(type: "INTEGER", nullable: false),
                    score = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblScores", x => new { x.seasonId, x.gameSeq, x.homeOrAway, x.inning });
                    table.ForeignKey(
                        name: "FK_tblScores_tblGame_seasonId_gameSeq",
                        columns: x => new { x.seasonId, x.gameSeq },
                        principalTable: "tblGame",
                        principalColumns: new[] { "seasonId", "seq" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblEvent",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    paId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    InPlay = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsStrike = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBall = table.Column<bool>(type: "INTEGER", nullable: false),
                    PitcherId = table.Column<string>(type: "TEXT", nullable: true),
                    CatcherId = table.Column<string>(type: "TEXT", nullable: true),
                    BatterId = table.Column<string>(type: "TEXT", nullable: true),
                    pitchCode = table.Column<string>(type: "TEXT", nullable: true),
                    pitchType = table.Column<string>(type: "TEXT", nullable: true),
                    velocity = table.Column<decimal>(type: "TEXT", nullable: true),
                    coordX = table.Column<decimal>(type: "TEXT", nullable: true),
                    coordY = table.Column<decimal>(type: "TEXT", nullable: true),
                    PAId = table.Column<int>(type: "INTEGER", nullable: true)
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
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    runnerId = table.Column<string>(type: "TEXT", nullable: false),
                    IsOut = table.Column<bool>(type: "INTEGER", nullable: false),
                    Scored = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRBI = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsER = table.Column<bool>(type: "INTEGER", nullable: false),
                    ERPitcherId = table.Column<string>(type: "TEXT", nullable: true),
                    RunnerPlayerPlayerId = table.Column<string>(type: "TEXT", nullable: true)
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
                name: "IX_tblTeamGameStats_gameId_teamId",
                table: "tblTeamGameStats",
                columns: new[] { "gameId", "teamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblTeamSeasonRankingCache_seasonId_rank",
                table: "tblTeamSeasonRankingCache",
                columns: new[] { "seasonId", "rank" });

            migrationBuilder.CreateIndex(
                name: "IX_tblTeamSeasonRankingCache_seasonId_teamId",
                table: "tblTeamSeasonRankingCache",
                columns: new[] { "seasonId", "teamId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblBatterBox");

            migrationBuilder.DropTable(
                name: "tblBattingRankingCache");

            migrationBuilder.DropTable(
                name: "tblCodeBases");

            migrationBuilder.DropTable(
                name: "tblCodeEventType");

            migrationBuilder.DropTable(
                name: "tblCodeHardness");

            migrationBuilder.DropTable(
                name: "tblCodePitchCode");

            migrationBuilder.DropTable(
                name: "tblCodePitchType");

            migrationBuilder.DropTable(
                name: "tblCodeResult");

            migrationBuilder.DropTable(
                name: "tblCodeRunnerType");

            migrationBuilder.DropTable(
                name: "tblCodeTrajectory");

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
                name: "tblTeamGameStats");

            migrationBuilder.DropTable(
                name: "tblTeamSeasonRankingCache");

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
