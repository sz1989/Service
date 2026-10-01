using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Service.BAL.People;
using Service.Data;
using Service.Model;
using Service.Services;

namespace Service.Tests.BAL.People;

public class PersonServiceDeleteTests
{
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly IBackgroundTaskQueue _taskQueue = Substitute.For<IBackgroundTaskQueue>();
    private readonly IRedisPublisher _publisher = Substitute.For<IRedisPublisher>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    private PersonService CreateService() => new(
        NullLogger<PersonService>.Instance,
        _personRepo,
        _taskQueue,
        _cache,
        _publisher,
        _scopeFactory);

    [Fact]
    public async Task DeletePersonAsync_PersonExists_DeletesSavesAndReturnsTrue()
    {
        var existing = new Person { Id = 1, Name = "Existing" };
        _personRepo.GetByIdAsync(existing.Id).Returns(existing);

        var service = CreateService();

        var result = await service.DeletePersonAsync(existing.Id);

        result.Should().BeTrue();
        _personRepo.Received(1).Delete(existing);
        await _personRepo.Received(1).SaveAsync();
        await _cache.Received(1).RemoveAsync($"person:{existing.Id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePersonAsync_PersonDoesNotExist_ReturnsFalseAndDoesNotDelete()
    {
        const int id = 42;
        _personRepo.GetByIdAsync(id).Returns((Person?)null);

        var service = CreateService();

        var result = await service.DeletePersonAsync(id);

        result.Should().BeFalse();
        _personRepo.DidNotReceive().Delete(Arg.Any<Person>());
        await _personRepo.DidNotReceive().SaveAsync();
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
