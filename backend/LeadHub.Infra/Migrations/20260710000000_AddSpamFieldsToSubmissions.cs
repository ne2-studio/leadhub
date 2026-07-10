using FluentMigrator;

namespace LeadHub.Infra.Migrations;

[Migration(20260710000000)]
public class AddSpamFieldsToSubmissions : Migration
{
    public override void Up()
    {
        Alter.Table("Submissions")
            .AddColumn("Status").AsString(32).NotNullable().SetExistingRowsTo("Ham")
            .AddColumn("SpamScore").AsInt32().NotNullable().SetExistingRowsTo(0)
            .AddColumn("SpamReasons").AsCustom("jsonb").NotNullable().SetExistingRowsTo("[]");

        Create.Index("IX_Submissions_FormId_Status")
            .OnTable("Submissions")
            .OnColumn("FormId").Ascending()
            .OnColumn("Status").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_Submissions_FormId_Status").OnTable("Submissions");
        Delete.Column("Status").FromTable("Submissions");
        Delete.Column("SpamScore").FromTable("Submissions");
        Delete.Column("SpamReasons").FromTable("Submissions");
    }
}
