using System.Collections;
using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Tests.API.Integration;

public sealed class HealthApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserReadRepository>();
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IUserRegistrationReadRepository>();
            services.RemoveAll<IUserRegistrationRepository>();
            services.RemoveAll<IUserPasswordRepository>();
            services.RemoveAll<IUserAuthReadRepository>();
            services.RemoveAll<IAuthAuditReadRepository>();
            services.RemoveAll<IAuthAuditRepository>();
            services.RemoveAll<IRefreshTokenRepository>();
            services.RemoveAll<IAdminMfaRepository>();
            services.RemoveAll<IJwtTokenGenerator>();
            services.RemoveAll<IDbConnectionFactory>();

            services.AddSingleton<InMemoryAuthRepository>();
            services.AddSingleton<IUserReadRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserRegistrationReadRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserRegistrationRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserPasswordRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IUserAuthReadRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());
            services.AddSingleton<IAdminMfaRepository>(sp => sp.GetRequiredService<InMemoryAuthRepository>());

            services.AddSingleton<InMemoryRefreshTokenRepository>();
            services.AddSingleton<IRefreshTokenRepository>(sp => sp.GetRequiredService<InMemoryRefreshTokenRepository>());

            services.AddSingleton<InMemoryAuthAuditRepository>();
            services.AddSingleton<IAuthAuditRepository>(sp => sp.GetRequiredService<InMemoryAuthAuditRepository>());
            services.AddSingleton<IAuthAuditReadRepository>(sp => sp.GetRequiredService<InMemoryAuthAuditRepository>());
            services.AddSingleton<IJwtTokenGenerator, FakeJwtTokenGenerator>();

            services.AddSingleton<IDbConnectionFactory, HealthyDbConnectionFactory>();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private sealed class HealthyDbConnectionFactory : IDbConnectionFactory
    {
        public IDbConnection CreateConnection() => new HealthyDbConnection();
    }

    private sealed class HealthyDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public override string ConnectionString { get; set; } = "Server=fake;Database=PlanWriterDb";
        public override string Database => "PlanWriterDb";
        public override string DataSource => "FakeSqlServer";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close() => _state = ConnectionState.Closed;

        public override void Open() => _state = ConnectionState.Open;

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            Open();
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => new HealthyDbCommand(this);
    }

    private sealed class HealthyDbCommand(HealthyDbConnection connection) : DbCommand
    {
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; } = connection;
        protected override DbParameterCollection DbParameterCollection { get; } = new EmptyParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery() => 1;

        public override object ExecuteScalar() => 1;

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => throw new NotSupportedException();
    }

    private sealed class EmptyParameterCollection : DbParameterCollection
    {
        public override int Count => 0;
        public override object SyncRoot => new();
        public override int Add(object value) => throw new NotSupportedException();
        public override void AddRange(Array values) => throw new NotSupportedException();
        public override void Clear()
        {
        }

        public override bool Contains(object value) => false;
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) => throw new NotSupportedException();
        public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
        protected override DbParameter GetParameter(int index) => throw new NotSupportedException();
        protected override DbParameter GetParameter(string parameterName) => throw new NotSupportedException();
        public override int IndexOf(object value) => -1;
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => throw new NotSupportedException();
        public override void Remove(object value) => throw new NotSupportedException();
        public override void RemoveAt(int index) => throw new NotSupportedException();
        public override void RemoveAt(string parameterName) => throw new NotSupportedException();
        protected override void SetParameter(int index, DbParameter value) => throw new NotSupportedException();
        protected override void SetParameter(string parameterName, DbParameter value) => throw new NotSupportedException();
    }
}
