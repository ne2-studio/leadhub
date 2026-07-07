using FluentMigrator;

namespace LeadHub.Infra.Migrations;

[Migration(20260701000000)]
public class CreateFormsTable : Migration
{
    public override void Up()
    {
        Create.Table("Forms")
            .WithColumn("Id").AsString(64).PrimaryKey()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Slug").AsString(100).NotNullable()
            .WithColumn("Description").AsString(2000).Nullable()
            .WithColumn("NotificationsEnabled").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("NotificationEmail").AsString(320).Nullable()
            .WithColumn("ThankYouUrl").AsString(2000).Nullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

        Create.Index("UX_Forms_Slug")
            .OnTable("Forms")
            .OnColumn("Slug").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("Forms");
    }
}
