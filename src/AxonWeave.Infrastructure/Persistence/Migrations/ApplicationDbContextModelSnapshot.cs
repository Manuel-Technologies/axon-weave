using System;
using AxonWeave.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AxonWeave.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("AxonWeave.Domain.Entities.Conversation", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<Guid?>("CreatedByUserId").HasColumnType("uuid");
                    b.Property<string>("Title").IsRequired().HasMaxLength(160).HasColumnType("character varying(160)");
                    b.Property<int>("Type").HasColumnType("integer");
                    b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");
                    b.HasKey("Id");
                    b.ToTable("conversations");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.ConversationParticipant", b =>
                {
                    b.Property<Guid>("ConversationId").HasColumnType("uuid");
                    b.Property<Guid>("UserId").HasColumnType("uuid");
                    b.Property<DateTimeOffset>("JoinedAt").HasColumnType("timestamp with time zone");
                    b.HasKey("ConversationId", "UserId");
                    b.HasIndex("UserId");
                    b.ToTable("conversation_participants");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.MediaAsset", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<string>("ContentType").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)");
                    b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("OriginalFileName").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
                    b.Property<long>("SizeBytes").HasColumnType("bigint");
                    b.Property<string>("StoredFileName").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
                    b.Property<Guid>("UploadedByUserId").HasColumnType("uuid");
                    b.Property<string>("Url").IsRequired().HasMaxLength(2048).HasColumnType("character varying(2048)");
                    b.HasKey("Id");
                    b.HasIndex("UploadedByUserId");
                    b.ToTable("media_assets");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.Message", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<Guid>("ConversationId").HasColumnType("uuid");
                    b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<DateTimeOffset?>("DeletedAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("EncryptedContent").IsRequired().HasColumnType("text");
                    b.Property<bool>("IsDeletedForEveryone").HasColumnType("boolean");
                    b.Property<string>("MediaContentType").HasMaxLength(128).HasColumnType("character varying(128)");
                    b.Property<string>("MediaUrl").HasMaxLength(2048).HasColumnType("character varying(2048)");
                    b.Property<DateTimeOffset?>("ReadAt").HasColumnType("timestamp with time zone");
                    b.Property<Guid>("SenderId").HasColumnType("uuid");
                    b.HasKey("Id");
                    b.HasIndex("ConversationId", "CreatedAt");
                    b.HasIndex("SenderId");
                    b.ToTable("messages");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.MessageDelivery", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<DateTimeOffset?>("DeliveredAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("LastError").HasColumnType("text");
                    b.Property<Guid>("MessageId").HasColumnType("uuid");
                    b.Property<DateTimeOffset?>("ReadAt").HasColumnType("timestamp with time zone");
                    b.Property<int>("RetryCount").HasColumnType("integer");
                    b.Property<int>("Status").HasColumnType("integer");
                    b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");
                    b.Property<Guid>("UserId").HasColumnType("uuid");
                    b.HasKey("Id");
                    b.HasIndex("MessageId", "UserId").IsUnique();
                    b.HasIndex("UserId", "Status");
                    b.ToTable("message_deliveries");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.PendingOtp", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<int>("Attempts").HasColumnType("integer");
                    b.Property<string>("CodeHash").IsRequired().HasMaxLength(512).HasColumnType("character varying(512)");
                    b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<DateTimeOffset>("ExpiresAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("PhoneNumber").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
                    b.HasKey("Id");
                    b.HasIndex("PhoneNumber");
                    b.ToTable("pending_otps");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
                    b.Property<string>("PhoneNumber").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
                    b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");
                    b.HasKey("Id");
                    b.HasIndex("PhoneNumber").IsUnique();
                    b.ToTable("users");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.ConversationParticipant", b =>
                {
                    b.HasOne("AxonWeave.Domain.Entities.Conversation", "Conversation")
                        .WithMany("Participants")
                        .HasForeignKey("ConversationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AxonWeave.Domain.Entities.User", "User")
                        .WithMany("Conversations")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Conversation");
                    b.Navigation("User");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.MediaAsset", b =>
                {
                    b.HasOne("AxonWeave.Domain.Entities.User", "UploadedByUser")
                        .WithMany()
                        .HasForeignKey("UploadedByUserId");

                    b.Navigation("UploadedByUser");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.Message", b =>
                {
                    b.HasOne("AxonWeave.Domain.Entities.Conversation", "Conversation")
                        .WithMany("Messages")
                        .HasForeignKey("ConversationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AxonWeave.Domain.Entities.User", "Sender")
                        .WithMany("Messages")
                        .HasForeignKey("SenderId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Conversation");
                    b.Navigation("Sender");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.MessageDelivery", b =>
                {
                    b.HasOne("AxonWeave.Domain.Entities.Message", "Message")
                        .WithMany("Deliveries")
                        .HasForeignKey("MessageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AxonWeave.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Message");
                    b.Navigation("User");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.Conversation", b =>
                {
                    b.Navigation("Messages");
                    b.Navigation("Participants");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.Message", b =>
                {
                    b.Navigation("Deliveries");
                });

            modelBuilder.Entity("AxonWeave.Domain.Entities.User", b =>
                {
                    b.Navigation("Conversations");
                    b.Navigation("Messages");
                });
#pragma warning restore 612, 618
        }
    }
}
