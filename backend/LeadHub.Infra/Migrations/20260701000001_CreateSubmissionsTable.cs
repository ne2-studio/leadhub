using System.Data;
using FluentMigrator;

namespace LeadHub.Infra.Migrations;

[Migration(20260701000001)]
public class CreateSubmissionsTable : Migration
{
    public override void Up()
    {
        Create.Table("Submissions")
            .WithColumn("Id").AsString(64).PrimaryKey()
            .WithColumn("FormId").AsString(64).NotNullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("IpAddress").AsString(64).Nullable()
            .WithColumn("UserAgent").AsString(512).Nullable()
            .WithColumn("Payload").AsCustom("jsonb").NotNullable();

        Create.ForeignKey("FK_Submissions_Forms")
            .FromTable("Submissions").ForeignColumn("FormId")
            .ToTable("Forms").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.Index("IX_Submissions_FormId_CreatedAt")
            .OnTable("Submissions")
            .OnColumn("FormId").Ascending()
            .OnColumn("CreatedAt").Descending();
    }

    public override void Down()
    {
        Delete.Table("Submissions");
    }
}
