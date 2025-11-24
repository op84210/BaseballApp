using BaseballApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BaseballApp.Data;

public class BaseballDbContext : DbContext
{
    public BaseballDbContext(DbContextOptions<BaseballDbContext> options) 
        : base(options) 
    { }

    // 主檔資料表
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Stadium> Stadiums { get; set; }
    public DbSet<Batter> Batters { get; set; }
    public DbSet<Pitcher> Pitchers { get; set; }
    public DbSet<PlayerTeam> PlayerTeams { get; set; }

    // 比賽相關資料表
    public DbSet<Game> Games { get; set; }
    public DbSet<Scores> Scores { get; set; }
    public DbSet<BatterBox> BatterBoxes { get; set; }
    public DbSet<PitcherBox> PitcherBoxes { get; set; }

    // 打席相關資料表
    public DbSet<PA> PAs { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Runner> Runners { get; set; }

    // 排行榜快取資料表
    public DbSet<BattingRankingCache> BattingRankingCaches { get; set; }
    public DbSet<PitchingRankingCache> PitchingRankingCaches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // tblSeason
        modelBuilder.Entity<Season>(entity =>
        {
            entity.ToTable("tblSeason");
            entity.HasKey(e => e.SeasonId);
            entity.Property(e => e.SeasonId).HasColumnName("seasonId");
            entity.Property(e => e.SeasonName).HasColumnName("season");
        });

        // tblTeam
        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("tblTeam");
            entity.HasKey(e => e.TeamId);
            entity.Property(e => e.TeamId).HasColumnName("teamId");
            entity.Property(e => e.TeamName).HasColumnName("teamName");
        });

        // tblStadium
        modelBuilder.Entity<Stadium>(entity =>
        {
            entity.ToTable("tblStadium");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("stadiumId");
            entity.Property(e => e.stadium).HasColumnName("stadiumName");
        });

        // tblBatter
        modelBuilder.Entity<Batter>(entity =>
        {
            entity.ToTable("tblBatter");
            entity.HasKey(e => e.PlayerId);
            entity.Property(e => e.PlayerId).HasColumnName("playerId");
            entity.Property(e => e.PlayerName).HasColumnName("playerName");
            entity.Property(e => e.PlayerNumber).HasColumnName("playerNumber");
        });

        // tblPitcher
        modelBuilder.Entity<Pitcher>(entity =>
        {
            entity.ToTable("tblPitcher");
            entity.HasKey(e => e.PlayerId);
            entity.Property(e => e.PlayerId).HasColumnName("playerId");
            entity.Property(e => e.PlayerName).HasColumnName("playerName");
            entity.Property(e => e.PlayerNumber).HasColumnName("playerNumber");
        });

        // tblPlayerTeam
        modelBuilder.Entity<PlayerTeam>(entity =>
        {
            entity.ToTable("tblPlayerTeam");
            entity.HasKey(e => new { e.Id });
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.PlayerId).HasColumnName("playerId");
            entity.Property(e => e.TeamId).HasColumnName("teamId");
            entity.Property(e => e.SeasonId).HasColumnName("seasonId");
            entity.Property(e => e.PlayerNumber).HasColumnName("playerNumber");
            entity.Property(e => e.StartDate).HasColumnName("startDate");
            entity.Property(e => e.EndDate).HasColumnName("endDate");
            entity.Property(e => e.IsActive).HasColumnName("isActive");

            // 關聯關係：PlayerTeam -> Batter
            entity.HasOne(pt => pt.Batter)
                .WithMany(b => b.PlayerTeams)
                .HasForeignKey(pt => pt.PlayerId)
                .HasPrincipalKey(b => b.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 關聯關係：PlayerTeam -> Team
            entity.HasOne(pt => pt.Team)
                .WithMany(t => t.PlayerTeams)
                .HasForeignKey(pt => pt.TeamId)
                .HasPrincipalKey(t => t.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // 關聯關係：PlayerTeam -> Season
            entity.HasOne(pt => pt.Season)
                .WithMany()
                .HasForeignKey(pt => pt.SeasonId)
                .HasPrincipalKey(s => s.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // tblGame
        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("tblGame");
            entity.HasKey(e => new { e.SeasonId, e.Seq });
            entity.Property(e => e.SeasonId).HasColumnName("seasonId");
            entity.Property(e => e.Seq).HasColumnName("seq");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.StadiumId).HasColumnName("stadiumId");
            entity.Property(e => e.AwayTeamId).HasColumnName("awayTeamId");
            entity.Property(e => e.HomeTeamId).HasColumnName("homeTeamId");
        });

        // tblScores
        modelBuilder.Entity<Scores>(entity =>
        {
            entity.ToTable("tblScores");
            entity.HasKey(e => e.SeasonId);
            entity.Property(e => e.SeasonId).HasColumnName("seasonId");
            entity.Property(e => e.GameSeq).HasColumnName("gameSeq");
            entity.Property(e => e.HomeOrAway).HasColumnName("homeOrAway");
            entity.Property(e => e.Inning).HasColumnName("inning");
            entity.Property(e => e.Score).HasColumnName("score");
        });

        // tblBatterBox
        modelBuilder.Entity<BatterBox>(entity =>
        {
            entity.ToTable("tblBatterBox");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.SeasonId).HasColumnName("seasonId");
            entity.Property(e => e.GameSeq).HasColumnName("gameSeq");
            entity.Property(e => e.HomeOrAway).HasColumnName("homeOrAway");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.SubOrder).HasColumnName("subOrder");
            entity.Property(e => e.PlayerId).HasColumnName("playerId");
            entity.Property(e => e.PA).HasColumnName("PA");
            entity.Property(e => e.AB).HasColumnName("AB");
            entity.Property(e => e.R).HasColumnName("R");
            entity.Property(e => e.H).HasColumnName("H");
            entity.Property(e => e.RBI).HasColumnName("RBI");
            entity.Property(e => e.TwoB).HasColumnName("2B");
            entity.Property(e => e.ThreeB).HasColumnName("3B");
            entity.Property(e => e.HR).HasColumnName("HR");
            entity.Property(e => e.GIDP).HasColumnName("GIDP");
            entity.Property(e => e.DP).HasColumnName("DP");
            entity.Property(e => e.TP).HasColumnName("TP");
            entity.Property(e => e.BB).HasColumnName("BB");
            entity.Property(e => e.IBB).HasColumnName("IBB");
            entity.Property(e => e.HBP).HasColumnName("HBP");
            entity.Property(e => e.SO).HasColumnName("SO");
            entity.Property(e => e.SH).HasColumnName("SH");
            entity.Property(e => e.SF).HasColumnName("SF");
            entity.Property(e => e.E).HasColumnName("E");
            entity.Property(e => e.SB).HasColumnName("SB");
            entity.Property(e => e.CS).HasColumnName("CS");

            // 關聯關係：BatterBox -> Game (複合鍵)
            entity.HasOne(bb => bb.Game)
                .WithMany()
                .HasForeignKey(bb => new { bb.SeasonId, bb.GameSeq })
                .HasPrincipalKey(g => new { g.SeasonId, g.Seq })
                .OnDelete(DeleteBehavior.Restrict);

            // 關聯關係：BatterBox -> Batter
            entity.HasOne(bb => bb.Player)
                .WithMany()
                .HasForeignKey(bb => bb.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // tblPitcherBox
        modelBuilder.Entity<PitcherBox>(entity =>
        {
            entity.ToTable("tblPitcherBox");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.SeasonId).HasColumnName("seasonId");
            entity.Property(e => e.GameSeq).HasColumnName("gameSeq");
            entity.Property(e => e.HomeOrAway).HasColumnName("homeOrAway");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.PlayerId).HasColumnName("playerId");
            entity.Property(e => e.IPOuts).HasColumnName("IPOuts");
            entity.Property(e => e.NP).HasColumnName("NP");
            entity.Property(e => e.BF).HasColumnName("BF");
            entity.Property(e => e.H).HasColumnName("H");
            entity.Property(e => e.HR).HasColumnName("HR");
            entity.Property(e => e.BB).HasColumnName("BB");
            entity.Property(e => e.IBB).HasColumnName("IBB");
            entity.Property(e => e.HB).HasColumnName("HB");
            entity.Property(e => e.SO).HasColumnName("SO");
            entity.Property(e => e.R).HasColumnName("R");
            entity.Property(e => e.ER).HasColumnName("ER");

            // 關聯關係：PitcherBox -> Game (複合鍵)
            entity.HasOne(pb => pb.Game)
                .WithMany()
                .HasForeignKey(pb => new { pb.SeasonId, pb.GameSeq })
                .HasPrincipalKey(g => new { g.SeasonId, g.Seq })
                .OnDelete(DeleteBehavior.Restrict);

            // 關聯關係：PitcherBox -> Pitcher
            entity.HasOne(pb => pb.Player)
                .WithMany()
                .HasForeignKey(pb => pb.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // tblPA
        modelBuilder.Entity<PA>(entity =>
        {
            entity.ToTable("tblPA");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.SeasonId).HasColumnName("seasonId");
            entity.Property(e => e.GameSeq).HasColumnName("gameSeq");
            entity.Property(e => e.HomeOrAway).HasColumnName("homeOrAway");
            entity.Property(e => e.Inning).HasColumnName("inning");
            entity.Property(e => e.PaSeq).HasColumnName("paSeq");
            entity.Property(e => e.BatterId).HasColumnName("batterId");
            entity.Property(e => e.PitcherId).HasColumnName("pitcherId");
            entity.Property(e => e.CatcherId).HasColumnName("catcherId");
            entity.Property(e => e.Bases).HasColumnName("bases");
            entity.Property(e => e.EndBases).HasColumnName("endBases");

            // 關聯關係：PA -> Game (複合鍵)
            entity.HasOne<Game>()
                .WithMany()
                .HasForeignKey(pa => new { pa.SeasonId, pa.GameSeq })
                .HasPrincipalKey(g => new { g.SeasonId, g.Seq })
                .OnDelete(DeleteBehavior.Restrict);
        });

        // tblEvent
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("tblEvent");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.PaId).HasColumnName("paId");
            entity.Property(e => e.PitchCode).HasColumnName("pitchCode");
            entity.Property(e => e.PitchType).HasColumnName("pitchType");
            entity.Property(e => e.Velocity).HasColumnName("velocity");
            entity.Property(e => e.CoordX).HasColumnName("coordX");
            entity.Property(e => e.CoordY).HasColumnName("coordY");

            // 關聯關係：Event -> PA
            entity.HasOne<PA>()
                .WithMany()
                .HasForeignKey(e => e.PaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // tblRunner
        modelBuilder.Entity<Runner>(entity =>
        {
            entity.ToTable("tblRunner");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.RunnerId).HasColumnName("runnerId");
        });

        // tblBattingRankingCache
        modelBuilder.Entity<BattingRankingCache>(entity =>
        {
            entity.ToTable("tblBattingRankingCache");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.SeasonId).HasColumnName("seasonId");
            entity.Property(e => e.PlayerId).HasColumnName("playerId");
            entity.Property(e => e.PlayerName).HasColumnName("playerName");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.Games).HasColumnName("games");
            entity.Property(e => e.PA).HasColumnName("pa");
            entity.Property(e => e.AB).HasColumnName("ab");
            entity.Property(e => e.H).HasColumnName("h");
            entity.Property(e => e.TwoB).HasColumnName("twoB");
            entity.Property(e => e.ThreeB).HasColumnName("threeB");
            entity.Property(e => e.HR).HasColumnName("hr");
            entity.Property(e => e.RBI).HasColumnName("rbi");
            entity.Property(e => e.R).HasColumnName("r");
            entity.Property(e => e.SO).HasColumnName("so");
            entity.Property(e => e.BB).HasColumnName("bb");
            entity.Property(e => e.SB).HasColumnName("sb");
            entity.Property(e => e.AVG).HasColumnName("avg").HasColumnType("decimal(5,3)");
            entity.Property(e => e.OBP).HasColumnName("obp").HasColumnType("decimal(5,3)");
            entity.Property(e => e.SLG).HasColumnName("slg").HasColumnType("decimal(5,3)");
            entity.Property(e => e.OPS).HasColumnName("ops").HasColumnType("decimal(5,3)");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");

            // 建立複合索引：seasonId + playerId
            entity.HasIndex(e => new { e.SeasonId, e.PlayerId }).IsUnique();
            // 建立索引：seasonId + rank
            entity.HasIndex(e => new { e.SeasonId, e.Rank });
        });

        // tblPitchingRankingCache
        modelBuilder.Entity<PitchingRankingCache>(entity =>
        {
            entity.ToTable("tblPitchingRankingCache");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.SeasonId).HasColumnName("seasonId");
            entity.Property(e => e.PlayerId).HasColumnName("playerId");
            entity.Property(e => e.PlayerName).HasColumnName("playerName");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.Games).HasColumnName("games");
            entity.Property(e => e.IP).HasColumnName("ip").HasColumnType("decimal(5,1)");
            entity.Property(e => e.IPOuts).HasColumnName("ipOuts");
            entity.Property(e => e.H).HasColumnName("h");
            entity.Property(e => e.HR).HasColumnName("hr");
            entity.Property(e => e.BB).HasColumnName("bb");
            entity.Property(e => e.SO).HasColumnName("so");
            entity.Property(e => e.R).HasColumnName("r");
            entity.Property(e => e.ER).HasColumnName("er");
            entity.Property(e => e.W).HasColumnName("w");
            entity.Property(e => e.L).HasColumnName("l");
            entity.Property(e => e.ERA).HasColumnName("era").HasColumnType("decimal(5,2)");
            entity.Property(e => e.WHIP).HasColumnName("whip").HasColumnType("decimal(5,2)");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");

            // 建立複合索引：seasonId + playerId
            entity.HasIndex(e => new { e.SeasonId, e.PlayerId }).IsUnique();
            // 建立索引：seasonId + rank
            entity.HasIndex(e => new { e.SeasonId, e.Rank });
        });
    }
}
