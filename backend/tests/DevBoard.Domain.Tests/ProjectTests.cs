using DevBoard.Domain.Entities;
using DevBoard.Domain.Exceptions;

namespace DevBoard.Domain.Tests;

public sealed class ProjectTests
{
    [Fact]
    public void CreateProject_WithValidData_SetsFields()
    {
        var createdAt = new DateTime(2026, 2, 8, 12, 0, 0, DateTimeKind.Utc);

        var project = new Project(
            name: "DevBoard",
            repoOwner: "carra",
            repoName: "devboard",
            gitHubTokenEncrypted: "encrypted-token",
            createdAt: createdAt);

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("DevBoard", project.Name);
        Assert.Equal("carra", project.RepoOwner);
        Assert.Equal("devboard", project.RepoName);
        Assert.Equal("encrypted-token", project.GitHubTokenEncrypted);
        Assert.Equal(createdAt, project.CreatedAt);
    }

    [Fact]
    public void CreateProject_WithInvalidName_ThrowsDomainException()
    {
        var action = () => new Project(
            name: " ",
            repoOwner: "carra",
            repoName: "devboard",
            gitHubTokenEncrypted: "encrypted-token");

        var exception = Assert.Throws<DomainException>(action);
        Assert.Equal("name is required.", exception.Message);
    }
}
