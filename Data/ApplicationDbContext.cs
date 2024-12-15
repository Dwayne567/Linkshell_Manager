using LinkshellManager.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinkshellManager.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Linkshell> Linkshells { get; set; }
        public DbSet<Invite> Invites { get; set; }
        public DbSet<AppUserLinkshell> AppUserLinkshells { get; set; }
        public DbSet<DkpAudit> DkpAudits { get; set; }
        public DbSet<DkpLedger> DkpLedgers { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Income> Incomes { get; set; }
        public DbSet<Tod> Tods { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventHistory> EventHistories { get; set; }
        public DbSet<AppUserEvent> AppUserEvents { get; set; }
        public DbSet<AppUserEventHistory> AppUserEventHistories { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<AppUserJob> AppUserJobs { get; set; }
        public DbSet<Auction> Auctions { get; set; }
        public DbSet<AuctionHistory> AuctionHistories { get; set; }
        public DbSet<AuctionItem> AuctionItems { get; set; }
        public DbSet<Bid> Bids { get; set; }
        public DbSet<TodLootDetail> TodLootDetails { get; set; }
        public DbSet<EventLootDetail> EventLootDetails { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<AppUserMessage> AppUserMessages { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Rule> Rules { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuring many-to-many relationship between AppUser and Linkshell
            modelBuilder.Entity<AppUserLinkshell>()
                .HasKey(ul => ul.Id);

            modelBuilder.Entity<AppUserLinkshell>()
                .HasOne(ul => ul.AppUser)
                .WithMany(u => u.AppUserLinkshells)
                .HasForeignKey(ul => ul.AppUserId);

            modelBuilder.Entity<AppUserLinkshell>()
                .HasOne(ul => ul.Linkshell)
                .WithMany(l => l.AppUserLinkshells)
                .HasForeignKey(ul => ul.LinkshellId);
            
            modelBuilder.Entity<DkpAudit>()
                .HasOne(da => da.AppUserLinkshell)
                .WithMany(aul => aul.DkpAudits)
                .HasForeignKey(da => da.AppUserLinkshellId);

            modelBuilder.Entity<DkpLedger>()
                .HasOne(da => da.AppUserLinkshell)
                .WithMany(aul => aul.DkpLedgers)
                .HasForeignKey(da => da.AppUserLinkshellId);


            // Configuring one-to-many relationships
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Linkshell)
                .WithMany(ls => ls.Items)
                .HasForeignKey(i => i.LinkshellId);
            
            modelBuilder.Entity<Income>()
                .HasOne(i => i.Linkshell)
                .WithMany(ls => ls.Incomes)
                .HasForeignKey(i => i.LinkshellId);

            modelBuilder.Entity<Tod>()
                .HasOne(t => t.Linkshell)
                .WithMany(ls => ls.Tods)
                .HasForeignKey(t => t.LinkshellId);

            modelBuilder.Entity<Auction>()
                .HasOne(a => a.Linkshell)
                .WithMany(ls => ls.Auctions)
                .HasForeignKey(a => a.LinkshellId);

            modelBuilder.Entity<Announcement>()
                .HasOne(i => i.Linkshell)
                .WithMany(ls => ls.Announcements)
                .HasForeignKey(i => i.LinkshellId);

            modelBuilder.Entity<Rule>()
                .HasOne(i => i.Linkshell)
                .WithMany(ls => ls.Rules)
                .HasForeignKey(i => i.LinkshellId);

            modelBuilder.Entity<AuctionItem>()
                .HasOne(i => i.Auction)
                .WithMany(a => a.AuctionItems)
                .HasForeignKey(i => i.AuctionId);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Linkshell)
                .WithMany(ls => ls.Events)
                .HasForeignKey(e => e.LinkshellId);

            modelBuilder.Entity<AppUserEvent>()
                .HasOne(e => e.Event)
                .WithMany(ev => ev.AppUserEvents)
                .HasForeignKey(e => e.EventId);

            modelBuilder.Entity<EventHistory>()
                .HasOne(e => e.Linkshell)
                .WithMany(ls => ls.EventHistories)
                .HasForeignKey(e => e.LinkshellId);

            modelBuilder.Entity<AppUserEventHistory>()
                .HasOne(e => e.EventHistory)
                .WithMany(eh => eh.AppUserEventHistories)
                .HasForeignKey(e => e.EventHistoryId);

            modelBuilder.Entity<Job>()
                .HasOne(j => j.Event)
                .WithMany(e => e.Jobs)
                .HasForeignKey(j => j.EventId);

            modelBuilder.Entity<TodLootDetail>()
                .HasOne(e => e.Tod)
                .WithMany(ld => ld.TodLootDetails)
                .HasForeignKey(e => e.TodId);

            modelBuilder.Entity<EventLootDetail>()
                .HasOne(t => t.Event)
                .WithMany(ld => ld.EventLootDetails)
                .HasForeignKey(e => e.EventId);
                
            // Configuring many-to-many relationship between AppUser and Message through AppUserMessage
            modelBuilder.Entity<AppUserMessage>()
                .HasKey(um => um.Id);

            modelBuilder.Entity<AppUserMessage>()
                .HasOne(um => um.AppUser)
                .WithMany(u => u.AppUserMessages)
                .HasForeignKey(um => um.AppUserId);

            modelBuilder.Entity<AppUserMessage>()
                .HasOne(um => um.Message)
                .WithMany(m => m.AppUserMessages)
                .HasForeignKey(um => um.MessageId);
            // Configuring properties
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.StartTime).HasColumnType("datetime");
                entity.Property(e => e.EndTime).HasColumnType("datetime");
                entity.Property(e => e.CommencementStartTime).HasColumnType("datetime");
                entity.Property(e => e.TimeStamp).HasColumnType("datetime");
            });

            modelBuilder.Entity<AppUserEvent>(entity =>
            {
                entity.Property(e => e.StartTime).HasColumnType("datetime");
                entity.Property(e => e.EndTime).HasColumnType("datetime");
                entity.Property(e => e.PauseTime).HasColumnType("datetime");
                entity.Property(e => e.ResumeTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<AppUserEventHistory>(entity =>
            {
                entity.Property(e => e.StartTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<EventHistory>(entity =>
            {
                entity.Property(e => e.StartTime).HasColumnType("datetime");
                entity.Property(e => e.EndTime).HasColumnType("datetime");
                entity.Property(e => e.CommencementStartTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<Tod>(entity =>
            {
                entity.Property(e => e.Time).HasColumnType("datetime");
                entity.Property(e => e.RepopTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<Auction>(entity =>
            {
                entity.Property(e => e.StartTime).HasColumnType("datetime");
                entity.Property(e => e.EndTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<AuctionItem>(entity =>
            {
                entity.Property(e => e.StartTime).HasColumnType("datetime");
                entity.Property(e => e.EndTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<AuctionHistory>(entity =>
            {
                entity.Property(e => e.StartTime).HasColumnType("datetime");
                entity.Property(e => e.EndTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<AppUserMessage>(entity =>
            {
                entity.Property(e => e.TimeStamp).HasColumnType("datetime");
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.Property(e => e.TimeStamp).HasColumnType("datetime");
            });

            modelBuilder.Entity<DkpLedger>(entity =>
            {
                entity.Property(e => e.TimeStamp).HasColumnType("datetime");
            });
        }
    }
}