using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class SuministroNisGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_AssignsCurrentEfTransactionToSequenceCommand()
    {
        var connection = new RecordingDbConnection();
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer(connection)
            .Options;
        await using var dbContext = new PanyebarDbContext(options);
        await using var transaction = new RecordingDbTransaction(connection);
        dbContext.Database.UseTransaction(transaction);

        var nis = await new SuministroNisGenerator(dbContext).GenerateAsync();

        Assert.Equal("PAN-000001", nis);
        Assert.Same(transaction, connection.LastCommand!.Transaction);
    }

    [Fact]
    public async Task GenerateAsync_WorksWithoutCurrentEfTransaction()
    {
        var connection = new RecordingDbConnection();
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer(connection)
            .Options;
        await using var dbContext = new PanyebarDbContext(options);

        var nis = await new SuministroNisGenerator(dbContext).GenerateAsync();

        Assert.Equal("PAN-000001", nis);
        Assert.Null(connection.LastCommand!.Transaction);
    }

    private sealed class RecordingDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public RecordingDbCommand? LastCommand { get; private set; }
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "Panyebar";
        public override string DataSource => "test";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) { }

        public override void Close() => _state = ConnectionState.Closed;

        public override void Open() => _state = ConnectionState.Open;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            new RecordingDbTransaction(this);

        protected override DbCommand CreateDbCommand()
        {
            LastCommand = new RecordingDbCommand(this);
            return LastCommand;
        }
    }

    private sealed class RecordingDbTransaction : DbTransaction
    {
        private readonly DbConnection _connection;

        public RecordingDbTransaction(DbConnection connection)
        {
            _connection = connection;
        }

        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        protected override DbConnection DbConnection => _connection;
        public override void Commit() { }
        public override void Rollback() { }
    }

    private sealed class RecordingDbCommand : DbCommand
    {
        private readonly DbParameterCollection _parameters = new EmptyDbParameterCollection();
        private DbConnection? _connection;
        private DbTransaction? _transaction;

        public RecordingDbCommand(DbConnection connection)
        {
            _connection = connection;
        }

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection
        {
            get => _connection;
            set => _connection = value;
        }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction
        {
            get => _transaction;
            set => _transaction = value;
        }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar() => 1L;
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) => Task.FromResult<object?>(1L);
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }

    private sealed class EmptyDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = new();

        public override int Count => _items.Count;
        public override object SyncRoot => ((ICollection)_items).SyncRoot!;
        public override int Add(object? value) { _items.Add((DbParameter)value!); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (var value in values) Add(value); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object? value) => _items.Contains((DbParameter)value!);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object? value) => _items.IndexOf((DbParameter)value!);
        public override int IndexOf(string parameterName) => _items.FindIndex(parameter => parameter.ParameterName == parameterName);
        public override void Insert(int index, object? value) => _items.Insert(index, (DbParameter)value!);
        public override void Remove(object? value) => _items.Remove((DbParameter)value!);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }
}
