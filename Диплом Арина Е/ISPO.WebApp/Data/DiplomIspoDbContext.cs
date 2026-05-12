using ISPO.WebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ISPO.WebApp.Data;

public class DiplomIspoDbContext : DbContext
{
    public DiplomIspoDbContext(DbContextOptions<DiplomIspoDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<CourseMaterial> CourseMaterials => Set<CourseMaterial>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<LessonMaterial> LessonMaterials => Set<LessonMaterial>();
    public DbSet<LessonSubmission> LessonSubmissions => Set<LessonSubmission>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<NewsPost> NewsPosts => Set<NewsPost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>().ToTable("Students");
        modelBuilder.Entity<StudentGroup>().ToTable("StudentGroups");
        modelBuilder.Entity<Course>().ToTable("Courses");
        modelBuilder.Entity<CourseCategory>().ToTable("CourseCategories");
        modelBuilder.Entity<CourseMaterial>().ToTable("CourseMaterials");
        modelBuilder.Entity<Enrollment>().ToTable("Enrollments");
        modelBuilder.Entity<Schedule>().ToTable("Schedules");
        modelBuilder.Entity<LessonMaterial>().ToTable("LessonMaterials");
        modelBuilder.Entity<LessonSubmission>().ToTable("LessonSubmissions");
        modelBuilder.Entity<Attendance>().ToTable("Attendances");
        modelBuilder.Entity<Teacher>().ToTable("Teachers");
        modelBuilder.Entity<UserAccount>().ToTable("Users");
        modelBuilder.Entity<NewsPost>().ToTable("NewsPosts");

        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .Property(s => s.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<Student>()
            .HasOne(s => s.Group)
            .WithMany(g => g.Students)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Course>()
            .HasOne(c => c.Category)
            .WithMany(cc => cc.Courses)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Course>()
            .HasOne(c => c.Teacher)
            .WithMany(t => t.Courses)
            .HasForeignKey(c => c.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Course>()
            .Property(c => c.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<Teacher>()
            .Property(t => t.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<CourseMaterial>()
            .HasOne(cm => cm.Course)
            .WithMany(c => c.Materials)
            .HasForeignKey(cm => cm.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique();

        modelBuilder.Entity<Enrollment>()
            .Property(e => e.EnrolledAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Course)
            .WithMany(c => c.Schedules)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Group)
            .WithMany(g => g.Schedules)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Schedule>()
            .Property(s => s.LiveStatus)
            .HasMaxLength(20)
            .HasDefaultValue("planned");

        modelBuilder.Entity<LessonMaterial>()
            .HasOne(lm => lm.Schedule)
            .WithMany(s => s.LessonMaterials)
            .HasForeignKey(lm => lm.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LessonMaterial>()
            .Property(lm => lm.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<LessonSubmission>()
            .HasOne(ls => ls.Schedule)
            .WithMany(s => s.LessonSubmissions)
            .HasForeignKey(ls => ls.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LessonSubmission>()
            .HasOne(ls => ls.Student)
            .WithMany(s => s.LessonSubmissions)
            .HasForeignKey(ls => ls.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LessonSubmission>()
            .HasIndex(ls => new { ls.ScheduleId, ls.StudentId })
            .IsUnique();

        modelBuilder.Entity<LessonSubmission>()
            .Property(ls => ls.Status)
            .HasMaxLength(30)
            .HasDefaultValue("new");

        modelBuilder.Entity<LessonSubmission>()
            .Property(ls => ls.RevisionNumber)
            .HasDefaultValue(1);

        modelBuilder.Entity<LessonSubmission>()
            .Property(ls => ls.SubmittedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Schedule)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.ScheduleId, a.StudentId })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .Property(a => a.MarkedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<UserAccount>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<NewsPost>()
            .Property(n => n.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<NewsPost>()
            .Property(n => n.Body)
            .HasMaxLength(4000);

        modelBuilder.Entity<NewsPost>()
            .Property(n => n.ImageUrl)
            .HasMaxLength(500);

        modelBuilder.Entity<NewsPost>()
            .Property(n => n.CreatedBy)
            .HasMaxLength(180);

    }
}
