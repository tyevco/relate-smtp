using Relate.Smtp.Tests.Common.Fixtures;
using Xunit;

namespace Relate.Smtp.Tests.Integration;

/// <summary>
/// Collection definition for sharing PostgresContainerFixture across integration tests.
/// This needs to be defined in the test project for xUnit to discover it.
/// </summary>
[CollectionDefinition("PostgresDatabase")]
public class PostgresDatabaseCollection : ICollectionFixture<PostgresContainerFixture>
{
}
