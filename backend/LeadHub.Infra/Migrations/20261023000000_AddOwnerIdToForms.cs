using FluentMigrator;

namespace LeadHub.Infra.Migrations;

[Migration(20261023000000)]
public class AddOwnerIdToForms : Migration
{
    // Forms created before ownership existed all belonged to the single original administrator.
    private const string LegacyFormsOwnerId = "380703884445745923";

    public override void Up()
    {
        Alter.Table("Forms")
            .AddColumn("OwnerId").AsString(255).NotNullable().SetExistingRowsTo(LegacyFormsOwnerId);

        Create.Index("IX_Forms_OwnerId")
            .OnTable("Forms")
            .OnColumn("OwnerId").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_Forms_OwnerId").OnTable("Forms");
        Delete.Column("OwnerId").FromTable("Forms");
    }
}
