using DexieNETCloudPushServer.QuartzDBEntities;
using Microsoft.EntityFrameworkCore;

namespace DexieNETCloudPushServer.Quartz;

public class QuartzDBContext : DbContext
{
    public static string DbPath => "database";
    public static string DbName => "quartz.db";

    public QuartzDBContext()
    {
    }

    public QuartzDBContext(DbContextOptions<QuartzDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<QrtzBlobTrigger> QrtzBlobTriggers { get; set; }

    public virtual DbSet<QrtzCalendar> QrtzCalendars { get; set; }

    public virtual DbSet<QrtzCronTrigger> QrtzCronTriggers { get; set; }

    public virtual DbSet<QrtzFiredTrigger> QrtzFiredTriggers { get; set; }

    public virtual DbSet<QrtzJobDetail> QrtzJobDetails { get; set; }

    public virtual DbSet<QrtzLock> QrtzLocks { get; set; }

    public virtual DbSet<QrtzPausedTriggerGrp> QrtzPausedTriggerGrps { get; set; }

    public virtual DbSet<QrtzSchedulerState> QrtzSchedulerStates { get; set; }

    public virtual DbSet<QrtzSimpleTrigger> QrtzSimpleTriggers { get; set; }

    public virtual DbSet<QrtzSimpropTrigger> QrtzSimpropTriggers { get; set; }

    public virtual DbSet<QrtzTrigger> QrtzTriggers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Directory.CreateDirectory(DbPath);
        var dbFullName = Path.Combine(DbPath, DbName);
        optionsBuilder.UseSqlite($"Data Source={dbFullName}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QrtzBlobTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_BLOB_TRIGGERS");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName).HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup).HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.BlobData)
                .HasColumnType("bytea")
                .HasColumnName("BLOB_DATA");

            entity.HasOne(d => d.QrtzTrigger).WithOne(p => p.QrtzBlobTrigger)
                .HasForeignKey<QrtzBlobTrigger>(d => new { d.SchedName, d.TriggerName, d.TriggerGroup });
        });

        modelBuilder.Entity<QrtzCalendar>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.CalendarName });

            entity.ToTable("QRTZ_CALENDARS");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.CalendarName).HasColumnName("CALENDAR_NAME");
            entity.Property(e => e.Calendar)
                .HasColumnType("bytea")
                .HasColumnName("CALENDAR");
        });

        modelBuilder.Entity<QrtzCronTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_CRON_TRIGGERS");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName).HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup).HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.CronExpression).HasColumnName("CRON_EXPRESSION");
            entity.Property(e => e.TimeZoneId).HasColumnName("TIME_ZONE_ID");

            entity.HasOne(d => d.QrtzTrigger).WithOne(p => p.QrtzCronTrigger)
                .HasForeignKey<QrtzCronTrigger>(d => new { d.SchedName, d.TriggerName, d.TriggerGroup });
        });

        modelBuilder.Entity<QrtzFiredTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.EntryId });

            entity.ToTable("QRTZ_FIRED_TRIGGERS");

            entity.HasIndex(e => e.JobGroup, "IDX_QRTZ_FT_JOB_GROUP");

            entity.HasIndex(e => e.JobName, "IDX_QRTZ_FT_JOB_NAME");

            entity.HasIndex(e => e.RequestsRecovery, "IDX_QRTZ_FT_JOB_REQ_RECOVERY");

            entity.HasIndex(e => e.TriggerGroup, "IDX_QRTZ_FT_TRIG_GROUP");

            entity.HasIndex(e => e.InstanceName, "IDX_QRTZ_FT_TRIG_INST_NAME");

            entity.HasIndex(e => e.TriggerName, "IDX_QRTZ_FT_TRIG_NAME");

            entity.HasIndex(e => new { e.SchedName, e.TriggerName, e.TriggerGroup }, "IDX_QRTZ_FT_TRIG_NM_GP");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.EntryId).HasColumnName("ENTRY_ID");
            entity.Property(e => e.FiredTime)
                .HasColumnType("bigint")
                .HasColumnName("FIRED_TIME");
            entity.Property(e => e.InstanceName).HasColumnName("INSTANCE_NAME");
            entity.Property(e => e.IsNonconcurrent)
                .HasColumnType("bool")
                .HasColumnName("IS_NONCONCURRENT");
            entity.Property(e => e.JobGroup).HasColumnName("JOB_GROUP");
            entity.Property(e => e.JobName).HasColumnName("JOB_NAME");
            entity.Property(e => e.Priority).HasColumnName("PRIORITY");
            entity.Property(e => e.RequestsRecovery)
                .HasColumnType("bool")
                .HasColumnName("REQUESTS_RECOVERY");
            entity.Property(e => e.SchedTime)
                .HasColumnType("bigint")
                .HasColumnName("SCHED_TIME");
            entity.Property(e => e.State).HasColumnName("STATE");
            entity.Property(e => e.TriggerGroup).HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.TriggerName).HasColumnName("TRIGGER_NAME");
        });

        modelBuilder.Entity<QrtzJobDetail>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.JobName, e.JobGroup });

            entity.ToTable("QRTZ_JOB_DETAILS");

            entity.HasIndex(e => e.RequestsRecovery, "IDX_QRTZ_J_REQ_RECOVERY");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.JobName).HasColumnName("JOB_NAME");
            entity.Property(e => e.JobGroup).HasColumnName("JOB_GROUP");
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
            entity.Property(e => e.IsDurable)
                .HasColumnType("bool")
                .HasColumnName("IS_DURABLE");
            entity.Property(e => e.IsNonconcurrent)
                .HasColumnType("bool")
                .HasColumnName("IS_NONCONCURRENT");
            entity.Property(e => e.IsUpdateData)
                .HasColumnType("bool")
                .HasColumnName("IS_UPDATE_DATA");
            entity.Property(e => e.JobClassName).HasColumnName("JOB_CLASS_NAME");
            entity.Property(e => e.JobData)
                .HasColumnType("bytea")
                .HasColumnName("JOB_DATA");
            entity.Property(e => e.RequestsRecovery)
                .HasColumnType("bool")
                .HasColumnName("REQUESTS_RECOVERY");
        });

        modelBuilder.Entity<QrtzLock>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.LockName });

            entity.ToTable("QRTZ_LOCKS");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.LockName).HasColumnName("LOCK_NAME");
        });

        modelBuilder.Entity<QrtzPausedTriggerGrp>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerGroup });

            entity.ToTable("QRTZ_PAUSED_TRIGGER_GRPS");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerGroup).HasColumnName("TRIGGER_GROUP");
        });

        modelBuilder.Entity<QrtzSchedulerState>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.InstanceName });

            entity.ToTable("QRTZ_SCHEDULER_STATE");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.InstanceName).HasColumnName("INSTANCE_NAME");
            entity.Property(e => e.CheckinInterval)
                .HasColumnType("bigint")
                .HasColumnName("CHECKIN_INTERVAL");
            entity.Property(e => e.LastCheckinTime)
                .HasColumnType("bigint")
                .HasColumnName("LAST_CHECKIN_TIME");
        });

        modelBuilder.Entity<QrtzSimpleTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_SIMPLE_TRIGGERS");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName).HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup).HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.RepeatCount)
                .HasColumnType("bigint")
                .HasColumnName("REPEAT_COUNT");
            entity.Property(e => e.RepeatInterval)
                .HasColumnType("bigint")
                .HasColumnName("REPEAT_INTERVAL");
            entity.Property(e => e.TimesTriggered)
                .HasColumnType("bigint")
                .HasColumnName("TIMES_TRIGGERED");

            entity.HasOne(d => d.QrtzTrigger).WithOne(p => p.QrtzSimpleTrigger)
                .HasForeignKey<QrtzSimpleTrigger>(d => new { d.SchedName, d.TriggerName, d.TriggerGroup });
        });

        modelBuilder.Entity<QrtzSimpropTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_SIMPROP_TRIGGERS");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName).HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup).HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.BoolProp1)
                .HasColumnType("bool")
                .HasColumnName("BOOL_PROP_1");
            entity.Property(e => e.BoolProp2)
                .HasColumnType("bool")
                .HasColumnName("BOOL_PROP_2");
            entity.Property(e => e.DecProp1)
                .HasColumnType("numeric")
                .HasColumnName("DEC_PROP_1");
            entity.Property(e => e.DecProp2)
                .HasColumnType("numeric")
                .HasColumnName("DEC_PROP_2");
            entity.Property(e => e.IntProp1).HasColumnName("INT_PROP_1");
            entity.Property(e => e.IntProp2).HasColumnName("INT_PROP_2");
            entity.Property(e => e.LongProp1)
                .HasColumnType("bigint")
                .HasColumnName("LONG_PROP_1");
            entity.Property(e => e.LongProp2)
                .HasColumnType("bigint")
                .HasColumnName("LONG_PROP_2");
            entity.Property(e => e.StrProp1).HasColumnName("STR_PROP_1");
            entity.Property(e => e.StrProp2).HasColumnName("STR_PROP_2");
            entity.Property(e => e.StrProp3).HasColumnName("STR_PROP_3");
            entity.Property(e => e.TimeZoneId).HasColumnName("TIME_ZONE_ID");

            entity.HasOne(d => d.QrtzTrigger).WithOne(p => p.QrtzSimpropTrigger)
                .HasForeignKey<QrtzSimpropTrigger>(d => new { d.SchedName, d.TriggerName, d.TriggerGroup });
        });

        modelBuilder.Entity<QrtzTrigger>(entity =>
        {
            entity.HasKey(e => new { e.SchedName, e.TriggerName, e.TriggerGroup });

            entity.ToTable("QRTZ_TRIGGERS");

            entity.HasIndex(e => e.NextFireTime, "IDX_QRTZ_T_NEXT_FIRE_TIME");

            entity.HasIndex(e => new { e.NextFireTime, e.TriggerState }, "IDX_QRTZ_T_NFT_ST");

            entity.HasIndex(e => e.TriggerState, "IDX_QRTZ_T_STATE");

            entity.HasIndex(e => new { e.SchedName, e.JobName, e.JobGroup },
                "IX_QRTZ_TRIGGERS_SCHED_NAME_JOB_NAME_JOB_GROUP");

            entity.Property(e => e.SchedName).HasColumnName("SCHED_NAME");
            entity.Property(e => e.TriggerName).HasColumnName("TRIGGER_NAME");
            entity.Property(e => e.TriggerGroup).HasColumnName("TRIGGER_GROUP");
            entity.Property(e => e.CalendarName).HasColumnName("CALENDAR_NAME");
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
            entity.Property(e => e.EndTime)
                .HasColumnType("bigint")
                .HasColumnName("END_TIME");
            entity.Property(e => e.JobData)
                .HasColumnType("bytea")
                .HasColumnName("JOB_DATA");
            entity.Property(e => e.JobGroup).HasColumnName("JOB_GROUP");
            entity.Property(e => e.JobName).HasColumnName("JOB_NAME");
            entity.Property(e => e.MisfireInstr)
                .HasColumnType("smallint")
                .HasColumnName("MISFIRE_INSTR");
            entity.Property(e => e.NextFireTime)
                .HasColumnType("bigint")
                .HasColumnName("NEXT_FIRE_TIME");
            entity.Property(e => e.PrevFireTime)
                .HasColumnType("bigint")
                .HasColumnName("PREV_FIRE_TIME");
            entity.Property(e => e.Priority).HasColumnName("PRIORITY");
            entity.Property(e => e.StartTime)
                .HasColumnType("bigint")
                .HasColumnName("START_TIME");
            entity.Property(e => e.TriggerState).HasColumnName("TRIGGER_STATE");
            entity.Property(e => e.TriggerType).HasColumnName("TRIGGER_TYPE");

            entity.HasOne(d => d.QrtzJobDetail).WithMany(p => p.QrtzTriggers)
                .HasForeignKey(d => new { d.SchedName, d.JobName, d.JobGroup });
        });
    }
}