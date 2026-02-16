using DevBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevBoard.Infrastructure.Persistence.Configurations;

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDeliveries");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.DeliveryId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(item => item.EventName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(item => item.ReceivedAt)
            .IsRequired();

        builder.HasIndex(item => item.DeliveryId)
            .IsUnique();
    }
}
