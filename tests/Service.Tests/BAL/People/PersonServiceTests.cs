using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Service.BAL.People;
using Service.Data;
using Service.Model;
using Service.Services;

namespace Service.Tests.BAL.People;

public class PersonServiceTests
{
    private readonly Mock<IPersonRepository> _personRepo = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Mock<IBackgroundTaskQueue> _taskQueue = new();
    private readonly Mock<IRedisPublisher> _publisher = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();

    private PersonService CreateService() => new(
        NullLogger<PersonService>.Instance,
        _personRepo.Object,
        _taskQueue.Object,
        _cache.Object,
        _publisher.Object,
        _scopeFactory.Object);

    [Fact]
    public async Task UpdatePersonAsync_PersonExists_UpdatesSavesAndReturnsPerson()
    {
        var existing = new Person { Id = 1, Name = "Old Name", Salary = 100m };
        var updated = new Person
        {
            Id = 1,
            Name = "New Name",
            DateOfBirth = new DateOnly(1990, 1, 15),
            ManagerId = 7,
            Salary = 200m
        };

        _personRepo.Setup(r => r.GetByIdAsync(updated.Id)).ReturnsAsync(existing);

        var service = CreateService();

        var result = await service.UpdatePersonAsync(updated);

        // The service updates the tracked entity (avoids an EF duplicate-key tracking conflict)
        // by copying values onto it, so it returns `existing`, not the incoming object.
        result.Should().BeSameAs(existing);
        result.Should().BeEquivalentTo(updated);
        _personRepo.Verify(r => r.Update(existing), Times.Once);
        _personRepo.Verify(r => r.SaveAsync(), Times.Once);
        _cache.Verify(c => c.RemoveAsync($"person:{updated.Id}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePersonAsync_PersonDoesNotExist_ReturnsNullAndDoesNotUpdate()
    {
        var updated = new Person { Id = 42, Name = "Ghost" };

        _personRepo.Setup(r => r.GetByIdAsync(updated.Id)).ReturnsAsync((Person?)null);

        var service = CreateService();

        var result = await service.UpdatePersonAsync(updated);

        result.Should().BeNull();
        _personRepo.Verify(r => r.Update(It.IsAny<Person>()), Times.Never);
        _personRepo.Verify(r => r.SaveAsync(), Times.Never);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
