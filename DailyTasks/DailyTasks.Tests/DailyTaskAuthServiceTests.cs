using DailyTasks.Application.DTOs.Auth;
using DailyTasks.Application.Interfaces;
using DailyTasks.Application.Services;
using DailyTasks.Core.Enums;
using DailyTasks.Core.Exceptions;
using DailyTasks.Core.Interfaces;
using Moq;

namespace DailyTasks.Tests;

public class DailyTaskAuthServiceTests
{
    private readonly Mock<IDailyTaskUserRepository> _users = new();
    private readonly Mock<IDailyTaskPasswordHasher> _hasher = new();
    private readonly Mock<IDailyTaskJwtTokenService> _tokens = new();
    private readonly Mock<IDailyTaskUnitOfWork> _uow = new();

    private DailyTaskAuthService Build(FakeCurrentUser current) =>
        new(_users.Object, _hasher.Object, _tokens.Object, current, _uow.Object);

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var user = TestData.User(1);
        _users.Setup(r => r.GetByUsernameAsync("user1")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("secret", user.PasswordHash)).Returns(true);
        _tokens.Setup(t => t.CreateToken(user)).Returns("jwt-token");

        var response = await Build(new FakeCurrentUser()).LoginAsync(new DailyTaskLoginRequest("User1", "secret"));

        Assert.Equal("jwt-token", response.Token);
        Assert.Equal(1, response.User.Id);
        Assert.Equal(DailyTaskAreas.Marketing, response.User.Area);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_Throws()
    {
        var user = TestData.User(1);
        _users.Setup(r => r.GetByUsernameAsync("user1")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            Build(new FakeCurrentUser()).LoginAsync(new DailyTaskLoginRequest("user1", "bad")));
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_Throws()
    {
        var user = TestData.User(1, active: false);
        _users.Setup(r => r.GetByUsernameAsync("user1")).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            Build(new FakeCurrentUser()).LoginAsync(new DailyTaskLoginRequest("user1", "secret")));
    }

    [Fact]
    public async Task ChangePasswordAsync_Valid_UpdatesHash()
    {
        var user = TestData.User(1);
        _users.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("old-password", user.PasswordHash)).Returns(true);
        _hasher.Setup(h => h.Hash("new-password")).Returns("new-hash");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await Build(new FakeCurrentUser { Id = 1 }).ChangePasswordAsync(
            new ChangePasswordRequest("old-password", "new-password"));

        Assert.Equal("new-hash", user.PasswordHash);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrent_Throws()
    {
        var user = TestData.User(1);
        _users.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Build(new FakeCurrentUser { Id = 1 }).ChangePasswordAsync(
                new ChangePasswordRequest("bad", "new-password")));
    }
}
