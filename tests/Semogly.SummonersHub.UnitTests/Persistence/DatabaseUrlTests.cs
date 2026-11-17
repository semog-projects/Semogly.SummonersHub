using Npgsql;
using Semogly.SummonersHub.Infrastructure.Persistence;

namespace Semogly.SummonersHub.UnitTests.Persistence;

public class DatabaseUrlTests
{
    [Fact]
    public void ToConnectionString_ShouldConvertNeonUrl()
    {
        var connectionString = DatabaseUrl.ToConnectionString(
            "postgresql://neondb_owner:s3cr%40t@ep-cool-name-123.sa-east-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Equal("ep-cool-name-123.sa-east-1.aws.neon.tech", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("neondb", builder.Database);
        Assert.Equal("neondb_owner", builder.Username);
        Assert.Equal("s3cr@t", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal(ChannelBinding.Require, builder.ChannelBinding);
    }

    [Fact]
    public void ToConnectionString_ShouldKeepExplicitPortAndIgnoreUnknownParameters()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            DatabaseUrl.ToConnectionString("postgres://user:pass@localhost:6543/app?sslmode=verify-full&application_name=x"));

        Assert.Equal(6543, builder.Port);
        Assert.Equal(SslMode.VerifyFull, builder.SslMode);
    }

    [Fact]
    public void ToConnectionString_ShouldRejectOtherSchemes() =>
        Assert.Throws<ArgumentException>(() => DatabaseUrl.ToConnectionString("mysql://user:pass@localhost/app"));
}
