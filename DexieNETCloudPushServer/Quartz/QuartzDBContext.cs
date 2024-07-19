using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.SQLite;
using Microsoft.EntityFrameworkCore;

namespace DexieNETCloudPushServer.Quartz;

public class QuartzDBContext : DbContext
{
    public static string DbPath => "database";
    public static string DbName => "quartz.db";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Adds Quartz.NET SQLite schema to EntityFrameworkCore
        modelBuilder.AddQuartz(builder => builder.UseSqlite());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        Directory.CreateDirectory(DbPath);
        var dbFullName = Path.Combine(DbPath, DbName);
        options.UseSqlite($"Data Source={dbFullName}");
    }
}