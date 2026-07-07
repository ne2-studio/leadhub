using NSubstitute;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests;

public class CachedFormRepositoryTests
{
    private readonly IFormRepository _repository;
    private readonly CachedFormRepository _cachedRepository;
    private readonly Form _testForm;

    public CachedFormRepositoryTests()
    {
        _repository = Substitute.For<IFormRepository>();
        _cachedRepository = new CachedFormRepository(_repository);
        _testForm = new Form("form-1", "Contact", "contact", null, false, null, null, DateTimeOffset.UtcNow, null);
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnFromCache_WhenItemIsCached()
    {
        _repository.GetBySlug(_testForm.Slug).Returns(_testForm);
        await _cachedRepository.GetBySlug(_testForm.Slug); // First call caches the item

        var result = await _cachedRepository.GetBySlug(_testForm.Slug);

        Assert.Equal(_testForm, result);
        await _repository.Received(1).GetBySlug(_testForm.Slug); // Only hit the repository once
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnFromRepository_WhenItemIsNotCached()
    {
        _repository.GetBySlug(_testForm.Slug).Returns(_testForm);

        var result = await _cachedRepository.GetBySlug(_testForm.Slug);

        Assert.Equal(_testForm, result);
        await _repository.Received(1).GetBySlug(_testForm.Slug);
    }

    [Fact]
    public async Task GetBySlug_ShouldNotCache_WhenRepositoryReturnsNull()
    {
        _repository.GetBySlug(_testForm.Slug).Returns((Form?)null);

        var result = await _cachedRepository.GetBySlug(_testForm.Slug);

        Assert.Null(result);
        await _repository.Received(1).GetBySlug(_testForm.Slug);
    }

    [Fact]
    public async Task Save_ShouldPopulateCache_WhenSavingNewItem()
    {
        await _cachedRepository.Save(_testForm);

        var byId = await _cachedRepository.GetById(_testForm.Id);
        var bySlug = await _cachedRepository.GetBySlug(_testForm.Slug);

        Assert.Equal(_testForm, byId);
        Assert.Equal(_testForm, bySlug);
        await _repository.Received(0).GetById(_testForm.Id);
        await _repository.Received(0).GetBySlug(_testForm.Slug);
    }

    [Fact]
    public async Task Save_ShouldEvictPreviousSlug_WhenSlugChanges()
    {
        await _cachedRepository.Save(_testForm);

        var renamed = _testForm with { Slug = "contact-us" };
        _repository.GetBySlug(_testForm.Slug).Returns((Form?)null);

        await _cachedRepository.Save(renamed);
        var oldSlugResult = await _cachedRepository.GetBySlug(_testForm.Slug);
        var newSlugResult = await _cachedRepository.GetBySlug("contact-us");

        Assert.Null(oldSlugResult);
        Assert.Equal(renamed, newSlugResult);
        await _repository.Received(1).GetBySlug(_testForm.Slug); // Falls through to repository after eviction
    }

    [Fact]
    public async Task Delete_ShouldEvictFromCache()
    {
        await _cachedRepository.Save(_testForm);
        _repository.GetById(_testForm.Id).Returns((Form?)null);

        await _cachedRepository.Delete(_testForm);
        var result = await _cachedRepository.GetById(_testForm.Id);

        Assert.Null(result);
        await _repository.Received(1).GetById(_testForm.Id); // Falls through to repository after eviction
    }

    [Fact]
    public async Task ExistsBySlug_ShouldAlwaysDelegateToRepository()
    {
        _repository.ExistsBySlug("contact", null).Returns(true);

        var result = await _cachedRepository.ExistsBySlug("contact");

        Assert.True(result);
        await _repository.Received(1).ExistsBySlug("contact", null);
    }
}
