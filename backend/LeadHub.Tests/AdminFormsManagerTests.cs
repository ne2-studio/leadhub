using Microsoft.Extensions.Logging.Abstractions;
using LeadHub.Application;
using LeadHub.Ports.Input;
using LeadHub.Ports.Output;
using LeadHub.Tests.Fakes;

namespace LeadHub.Tests;

public class AdminFormsManagerTests
{
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";
    private const string GeneratedId = "11111111-1111-1111-1111-111111111111";

    private readonly InMemoryFormRepository formRepository;
    private readonly AdminFormsManager manager;

    public AdminFormsManagerTests()
    {
        formRepository = new InMemoryFormRepository();
        manager = new AdminFormsManager(
            NullLogger<AdminFormsManager>.Instance, formRepository, new StaticIdGenerator(GeneratedId), new StaticClock());
    }

    [Fact]
    public async Task CreateForm_ShouldSucceed_WithValidInput()
    {
        var result = await manager.CreateForm(new CreateFormInput(
            OwnerId, "Contact Form", "contact", "Main contact form", false, null, "https://example.com/thanks"));

        Assert.True(result.IsSuccess);
        Assert.Equal(GeneratedId, result.Value.Id);
        Assert.Equal("contact", result.Value.Slug);
        Assert.Null(result.Value.UpdatedAt);

        var stored = await formRepository.GetById(GeneratedId);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task CreateForm_ShouldFail_WhenNameMissing()
    {
        var result = await manager.CreateForm(new CreateFormInput(OwnerId, "", "contact", null, false, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.InvalidFormConfiguration, result.Error.Code);
    }

    [Fact]
    public async Task CreateForm_ShouldFail_WhenSlugInvalid()
    {
        var result = await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "Not A Slug!", null, false, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.InvalidFormConfiguration, result.Error.Code);
    }

    [Fact]
    public async Task CreateForm_ShouldFail_WhenNotificationsEnabledWithoutEmail()
    {
        var result = await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, true, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.InvalidFormConfiguration, result.Error.Code);
    }

    [Fact]
    public async Task CreateForm_ShouldFail_WhenThankYouUrlInvalid()
    {
        var result = await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, "not-a-url"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.InvalidFormConfiguration, result.Error.Code);
    }

    [Fact]
    public async Task CreateForm_ShouldFail_WhenSlugAlreadyExists()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var result = await manager.CreateForm(new CreateFormInput(OwnerId, "Other Contact", "contact", null, false, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.SlugAlreadyExists, result.Error.Code);
    }

    [Fact]
    public async Task UpdateForm_ShouldFail_WhenFormDoesNotExist()
    {
        var result = await manager.UpdateForm(new UpdateFormInput(OwnerId, GeneratedId, "Contact", "contact", null, false, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.FormNotFound, result.Error.Code);
    }

    [Fact]
    public async Task UpdateForm_ShouldSucceed_AndSetUpdatedAt()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var result = await manager.UpdateForm(new UpdateFormInput(
            OwnerId, GeneratedId, "Contact Us", "contact-us", "New description", true, "owner@example.com", null));

        Assert.True(result.IsSuccess);
        Assert.Equal("contact-us", result.Value.Slug);
        Assert.Equal("Contact Us", result.Value.Name);
        Assert.NotNull(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task UpdateForm_ShouldAllowKeepingItsOwnSlug()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var result = await manager.UpdateForm(new UpdateFormInput(
            OwnerId, GeneratedId, "Contact Updated", "contact", null, false, null, null));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteForm_ShouldRemoveForm_WhenItExists()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var result = await manager.DeleteForm(new DeleteFormInput(OwnerId, GeneratedId));

        Assert.True(result.IsSuccess);
        Assert.Null(await formRepository.GetById(GeneratedId));
    }

    [Fact]
    public async Task DeleteForm_ShouldFail_WhenFormDoesNotExist()
    {
        var result = await manager.DeleteForm(new DeleteFormInput(OwnerId, GeneratedId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.FormNotFound, result.Error.Code);
    }

    [Fact]
    public async Task GetForm_ShouldFail_WhenFormDoesNotExist()
    {
        var result = await manager.GetForm(new GetFormInput(OwnerId, GeneratedId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.FormNotFound, result.Error.Code);
    }

    [Fact]
    public async Task ListForms_ShouldReturnAllForms()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var result = await manager.ListForms(new ListFormsInput(OwnerId));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task CreateForm_ShouldAssignCallerAsOwner()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var stored = await formRepository.GetById(GeneratedId);
        Assert.Equal(OwnerId, stored!.OwnerId);
    }

    [Fact]
    public async Task ListForms_ShouldOnlyReturnCallersForms()
    {
        await formRepository.Save(new Form(
            "other-form", OtherOwnerId, "Other", "other", null, false, null, null, DateTimeOffset.UtcNow, null));
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var result = await manager.ListForms(new ListFormsInput(OwnerId));

        Assert.True(result.IsSuccess);
        Assert.Equal(GeneratedId, Assert.Single(result.Value).Id);
    }

    [Fact]
    public async Task GetForm_ShouldFail_WhenFormBelongsToAnotherOwner()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var result = await manager.GetForm(new GetFormInput(OtherOwnerId, GeneratedId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.FormNotFound, result.Error.Code);
    }

    [Fact]
    public async Task UpdateForm_ShouldFail_WhenFormBelongsToAnotherOwner()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var result = await manager.UpdateForm(new UpdateFormInput(
            OtherOwnerId, GeneratedId, "Hijacked", "contact", null, false, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.FormNotFound, result.Error.Code);
        Assert.Equal("Contact", (await formRepository.GetById(GeneratedId))!.Name);
    }

    [Fact]
    public async Task UpdateForm_ShouldKeepOriginalOwner()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        await manager.UpdateForm(new UpdateFormInput(OwnerId, GeneratedId, "Contact Us", "contact", null, false, null, null));

        Assert.Equal(OwnerId, (await formRepository.GetById(GeneratedId))!.OwnerId);
    }

    [Fact]
    public async Task DeleteForm_ShouldFail_WhenFormBelongsToAnotherOwner()
    {
        await manager.CreateForm(new CreateFormInput(OwnerId, "Contact", "contact", null, false, null, null));

        var result = await manager.DeleteForm(new DeleteFormInput(OtherOwnerId, GeneratedId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.FormNotFound, result.Error.Code);
        Assert.NotNull(await formRepository.GetById(GeneratedId));
    }
}
