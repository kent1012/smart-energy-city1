using Microsoft.EntityFrameworkCore;
using SmartEnergyCity.Api.Models;

namespace SmartEnergyCity.Api.Data;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<GameState> GameStates => Set<GameState>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Generator> Generators => Set<Generator>();
    public DbSet<Substation> Substations => Set<Substation>();
    public DbSet<PowerLine> PowerLines => Set<PowerLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameState>().HasData(new GameState());

        modelBuilder.Entity<District>().HasData(
            new District { Id = 1, Name = "esil", NameRu = "Есиль", NameKz = "Есіл", CenterLat = 51.1694, CenterLng = 71.4491, Boundary = "[[51.175,71.436],[51.179,71.46],[51.163,71.467],[51.158,71.438]]" },
            new District { Id = 2, Name = "akmol", NameRu = "Акмол", NameKz = "Ақмол", CenterLat = 51.1400, CenterLng = 71.5200, Boundary = "[[51.147,71.505],[51.149,71.535],[51.134,71.536],[51.132,71.508]]" },
            new District { Id = 3, Name = "saryarka", NameRu = "Сарыарка", NameKz = "Сарыарқа", CenterLat = 51.1200, CenterLng = 71.4800, Boundary = "[[51.127,71.465],[51.13,71.495],[51.114,71.498],[51.109,71.468]]" },
            new District { Id = 4, Name = "aygarlyn", NameRu = "Айгарлын", NameKz = "Айғарлын", CenterLat = 51.1600, CenterLng = 71.3900, Boundary = "[[51.168,71.377],[51.171,71.404],[51.152,71.408],[51.149,71.382]]" },
            new District { Id = 5, Name = "almaty", NameRu = "Алматинский", NameKz = "Алматы", CenterLat = 51.0900, CenterLng = 71.5500, Boundary = "[[51.097,71.536],[51.101,71.564],[51.086,71.568],[51.081,71.541]]" }
        );

        modelBuilder.Entity<Building>()
            .HasIndex(x => x.ExternalId)
            .IsUnique();
    }
}
