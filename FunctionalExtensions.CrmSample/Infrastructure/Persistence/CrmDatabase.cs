using System.Collections.Immutable;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using FunctionalExtensions;
using FunctionalExtensions.CrmSample.Domain;
using FunctionalExtensions.Effects;
using Microsoft.Data.Sqlite;

namespace FunctionalExtensions.CrmSample.Infrastructure.Persistence;

/// <summary>
/// Thin SQLite wrapper that exposes transactional operations using the FunctionalExtensions effects layer.
/// </summary>
public sealed class CrmDatabase
{
    private const string DatabaseFileName = "crm.db";
    private readonly string _connectionString;

    public CrmDatabase(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataDirectory, DatabaseFileName),
            ForeignKeys = true
        };

        _connectionString = builder.ToString();
    }

    public SqliteConnection CreateConnection()
        => new(_connectionString);

    public TaskResult<Unit> InitializeAsync(CancellationToken cancellationToken = default)
    {
        return TaskResults.From(async () =>
        {
            await using var connection = CreateConnection();
            var pipeline = connection
                .ToStateTaskResult(async (conn, tx, ct) =>
                {
                    await ExecuteNonQueryAsync(conn, tx, Schema.CreateCustomers, ct).ConfigureAwait(false);
                    await ExecuteNonQueryAsync(conn, tx, Schema.CreateActivities, ct).ConfigureAwait(false);
                    await ExecuteNonQueryAsync(conn, tx, Schema.CreateAttachments, ct).ConfigureAwait(false);
                    return Unit.Value;
                }, cancellationToken)
                .Bind(_ => connection.CommitTransaction(dispose: true, cancellationToken));

            var result = await pipeline.RunAsync(DbTransactionState.Empty).ConfigureAwait(false);
            return result.IsSuccess
                ? Result<Unit>.Ok(Unit.Value)
                : Result<Unit>.Fail(result.Error ?? "Failed to initialize database.");
        });
    }

    public TaskResult<IReadOnlyList<Customer>> LoadCustomersAsync(CancellationToken cancellationToken = default)
    {
        return TaskResults.From(async () =>
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var customers = new List<Customer>();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT c.Id, c.Name, c.Email, c.SecondaryEmail, c.Phone,
                       c.AddressLine1, c.AddressLine2, c.City, c.Country, c.PostalCode,
                       c.PreferredChannel, c.ScoreNumerator, c.ScoreDenominator, c.TrendReal, c.TrendImaginary,
                       c.IsArchived
                FROM Customers c
                ORDER BY c.Name;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                customers.Add(MapCustomer(reader));
            }

            return Result<IReadOnlyList<Customer>>.Ok(customers);
        });
    }

    public TaskResult<Customer> UpsertCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        return TaskResults.From(async () =>
        {
            await using var connection = CreateConnection();
            var pipeline = connection
                .ToStateTaskResult((conn, tx, ct) => UpsertInternalAsync(conn, tx, customer, ct), cancellationToken)
                .Bind(_ => connection.CommitTransaction(dispose: true, cancellationToken));

            var result = await pipeline.RunAsync(DbTransactionState.Empty).ConfigureAwait(false);
            return result.IsSuccess
                ? Result<Customer>.Ok(customer)
                : Result<Customer>.Fail(result.Error ?? "Failed to save customer.");
        });
    }

    public TaskResult<Option<Customer>> TryLoadCustomerAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        return TaskResults.From(async () =>
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT c.Id, c.Name, c.Email, c.SecondaryEmail, c.Phone,
                       c.AddressLine1, c.AddressLine2, c.City, c.Country, c.PostalCode,
                       c.PreferredChannel, c.ScoreNumerator, c.ScoreDenominator, c.TrendReal, c.TrendImaginary,
                       c.IsArchived
                FROM Customers c
                WHERE c.Id = $id
                LIMIT 1;";
            command.Parameters.AddWithValue("$id", id.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return Result<Option<Customer>>.Ok(Option<Customer>.None);
            }

            var customer = MapCustomer(reader);
            var attachments = await LoadAttachmentsAsync((SqliteConnection)connection, id, cancellationToken).ConfigureAwait(false);
            return Result<Option<Customer>>.Ok(Option<Customer>.Some(customer with { Attachments = attachments }));
        });
    }

    public TaskResult<IReadOnlyList<CustomerAttachment>> LoadAttachmentsAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        return TaskResults.From(async () =>
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var attachments = await LoadAttachmentsAsync(connection, id, cancellationToken).ConfigureAwait(false);
            return Result<IReadOnlyList<CustomerAttachment>>.Ok(attachments);
        });
    }

    public TaskResult<Unit> DeleteCustomerAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        return TaskResults.From(async () =>
        {
            await using var connection = CreateConnection();
            var pipeline = connection
                .ToStateTaskResult((conn, tx, ct) => DeleteCustomerInternal(conn, tx, id, ct), cancellationToken)
                .Bind(_ => connection.CommitTransaction(dispose: true, cancellationToken));

            var result = await pipeline.RunAsync(DbTransactionState.Empty).ConfigureAwait(false);
            return result.IsSuccess
                ? Result<Unit>.Ok(Unit.Value)
                : Result<Unit>.Fail(result.Error ?? "Failed to delete customer.");
        });
    }

    public TaskResult<Unit> AppendActivityAsync(CustomerId id, Activity activity, CancellationToken cancellationToken = default)
    {
        return TaskResults.From(async () =>
        {
            await using var connection = CreateConnection();
            var pipeline = connection
                .ToStateTaskResult((conn, tx, ct) => InsertActivityAsync(conn, tx, id, activity, ct), cancellationToken)
                .Bind(_ => connection.CommitTransaction(dispose: true, cancellationToken));

            var result = await pipeline.RunAsync(DbTransactionState.Empty).ConfigureAwait(false);
            return result.IsSuccess
                ? Result<Unit>.Ok(Unit.Value)
                : Result<Unit>.Fail(result.Error ?? "Failed to append activity.");
        });
    }

    public TaskResult<Unit> UpsertAttachmentsAsync(CustomerId id, IEnumerable<CustomerAttachment> attachments, CancellationToken cancellationToken = default)
    {
        return TaskResults.From(async () =>
        {
            await using var connection = CreateConnection();
            var pipeline = connection
                .ToStateTaskResult((conn, tx, ct) => InsertAttachmentsAsync(conn, tx, id, attachments, ct), cancellationToken)
                .Bind(_ => connection.CommitTransaction(dispose: true, cancellationToken));

            var result = await pipeline.RunAsync(DbTransactionState.Empty).ConfigureAwait(false);
            return result.IsSuccess
                ? Result<Unit>.Ok(Unit.Value)
                : Result<Unit>.Fail(result.Error ?? "Failed to persist attachments.");
        });
    }

    public TaskResult<Unit> ArchiveCustomerAsync(CustomerId id, bool archived, CancellationToken cancellationToken = default)
    {
        return TaskResults.From(async () =>
        {
            await using var connection = CreateConnection();
            var pipeline = connection
                .ToStateTaskResult((conn, tx, ct) => ArchiveInternalAsync(conn, tx, id, archived, ct), cancellationToken)
                .Bind(_ => connection.CommitTransaction(dispose: true, cancellationToken));

            var result = await pipeline.RunAsync(DbTransactionState.Empty).ConfigureAwait(false);
            return result.IsSuccess
                ? Result<Unit>.Ok(Unit.Value)
                : Result<Unit>.Fail(result.Error ?? "Failed to archive customer.");
        });
    }

    private static Customer MapCustomer(SqliteDataReader reader)
    {
        var id = new CustomerId(reader.GetGuid(0));
        var name = reader.GetString(1);
        var email = reader.GetString(2);
        var secondaryRaw = reader.IsDBNull(3) ? null : reader.GetString(3);
        var phone = reader.GetString(4);
        var addressLine1 = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
        var addressLine2 = reader.IsDBNull(6) ? null : reader.GetString(6);
        var city = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
        var country = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);
        var postal = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
        var channel = reader.IsDBNull(10) ? null : reader.GetString(10);
        var scoreNumerator = reader.GetDecimal(11);
        var scoreDenominator = reader.GetDecimal(12);
        var trendReal = reader.GetDouble(13);
        var trendImaginary = reader.GetDouble(14);
        var archived = reader.GetBoolean(15);

        var address = string.IsNullOrWhiteSpace(addressLine1)
            ? Option<Address>.None
            : Option<Address>.Some(new Address(addressLine1, addressLine2, city, country, postal));

        var contact = new ContactInfo(phone, address, channel is null ? Option<string>.None : Option<string>.Some(channel));
        var score = new LeadScore(new FunctionalExtensions.Numerics.Rational<decimal>(scoreNumerator, scoreDenominator), new System.Numerics.Complex(trendReal, trendImaginary));

        return new Customer(
            id,
            name,
            email,
            secondaryRaw is null ? Option<string>.None : Option<string>.Some(secondaryRaw),
            contact,
            score,
            ImmutableArray<Activity>.Empty,
            ImmutableArray<CustomerAttachment>.Empty,
            archived);
    }

    private static Task<Unit> UpsertInternalAsync(DbConnection connection, DbTransaction transaction, Customer customer, CancellationToken cancellationToken)
    {
        var sqliteConnection = (SqliteConnection)connection;
        var sqliteTransaction = (SqliteTransaction)transaction;
        var command = sqliteConnection.CreateCommand();
        command.Transaction = sqliteTransaction;
        command.CommandText = @"
            INSERT INTO Customers (Id, Name, Email, SecondaryEmail, Phone, AddressLine1, AddressLine2, City, Country, PostalCode, PreferredChannel, ScoreNumerator, ScoreDenominator, TrendReal, TrendImaginary, IsArchived)
            VALUES ($id, $name, $email, $secondary, $phone, $line1, $line2, $city, $country, $postal, $channel, $scoreNumerator, $scoreDenominator, $trendReal, $trendImaginary, $archived)
            ON CONFLICT(Id) DO UPDATE SET
                Name=$name,
                Email=$email,
                SecondaryEmail=$secondary,
                Phone=$phone,
                AddressLine1=$line1,
                AddressLine2=$line2,
                City=$city,
                Country=$country,
                PostalCode=$postal,
                PreferredChannel=$channel,
                ScoreNumerator=$scoreNumerator,
                ScoreDenominator=$scoreDenominator,
                TrendReal=$trendReal,
                TrendImaginary=$trendImaginary,
                IsArchived=$archived;";

        command.Parameters.AddWithValue("$id", customer.Id.Value);
        command.Parameters.AddWithValue("$name", customer.Name);
        command.Parameters.AddWithValue("$email", customer.Email);
        command.Parameters.AddWithValue("$secondary", customer.SecondaryEmail.HasValue ? customer.SecondaryEmail.Value : (object?)DBNull.Value);
        command.Parameters.AddWithValue("$phone", customer.Contact.Phone);
        command.Parameters.AddWithValue("$line1", customer.Contact.Address.HasValue ? customer.Contact.Address.Value!.Line1 : string.Empty);
        command.Parameters.AddWithValue("$line2", customer.Contact.Address.HasValue ? customer.Contact.Address.Value!.Line2 ?? (object?)DBNull.Value : (object?)DBNull.Value);
        command.Parameters.AddWithValue("$city", customer.Contact.Address.HasValue ? customer.Contact.Address.Value!.City : string.Empty);
        command.Parameters.AddWithValue("$country", customer.Contact.Address.HasValue ? customer.Contact.Address.Value!.Country : string.Empty);
        command.Parameters.AddWithValue("$postal", customer.Contact.Address.HasValue ? customer.Contact.Address.Value!.PostalCode : string.Empty);
        command.Parameters.AddWithValue("$channel", customer.Contact.PreferredChannel.HasValue ? customer.Contact.PreferredChannel.Value : (object?)DBNull.Value);
        command.Parameters.AddWithValue("$scoreNumerator", customer.Score.Value.Numerator);
        command.Parameters.AddWithValue("$scoreDenominator", customer.Score.Value.Denominator);
        command.Parameters.AddWithValue("$trendReal", customer.Score.Trend.Real);
        command.Parameters.AddWithValue("$trendImaginary", customer.Score.Trend.Imaginary);
        command.Parameters.AddWithValue("$archived", customer.IsArchived ? 1 : 0);

        return ExecuteNonQueryAsync(command, cancellationToken);
    }

    private static Task<Unit> ArchiveInternalAsync(DbConnection connection, DbTransaction transaction, CustomerId id, bool archived, CancellationToken cancellationToken)
    {
        var sqliteConnection = (SqliteConnection)connection;
        var sqliteTransaction = (SqliteTransaction)transaction;
        var command = sqliteConnection.CreateCommand();
        command.Transaction = sqliteTransaction;
        command.CommandText = "UPDATE Customers SET IsArchived=$archived WHERE Id=$id;";
        command.Parameters.AddWithValue("$archived", archived ? 1 : 0);
        command.Parameters.AddWithValue("$id", id.Value);
        return ExecuteNonQueryAsync(command, cancellationToken);
    }

    private static Task<Unit> InsertActivityAsync(DbConnection connection, DbTransaction transaction, CustomerId id, Activity activity, CancellationToken cancellationToken)
    {
        var sqliteConnection = (SqliteConnection)connection;
        var sqliteTransaction = (SqliteTransaction)transaction;
        var command = sqliteConnection.CreateCommand();
        command.Transaction = sqliteTransaction;
        command.CommandText = @"
            INSERT INTO Activities (Id, CustomerId, Kind, OccurredAt, Summary, DurationSeconds)
            VALUES ($id, $customerId, $kind, $occurredAt, $summary, $duration);";

        command.Parameters.AddWithValue("$id", activity.Id);
        command.Parameters.AddWithValue("$customerId", id.Value);
        command.Parameters.AddWithValue("$kind", activity.Type.ToString());
        command.Parameters.AddWithValue("$occurredAt", activity.OccurredAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$summary", activity.Summary);
        command.Parameters.AddWithValue("$duration", activity.Duration.HasValue ? activity.Duration.Value.TotalSeconds : (object?)DBNull.Value);

        return ExecuteNonQueryAsync(command, cancellationToken);
    }

    private static async Task<Unit> InsertAttachmentsAsync(DbConnection connection, DbTransaction transaction, CustomerId id, IEnumerable<CustomerAttachment> attachments, CancellationToken cancellationToken)
    {
        var sqliteConnection = (SqliteConnection)connection;
        var sqliteTransaction = (SqliteTransaction)transaction;

        foreach (var attachment in attachments)
        {
            var command = sqliteConnection.CreateCommand();
            command.Transaction = sqliteTransaction;
            command.CommandText = @"
                INSERT INTO Attachments (Id, CustomerId, FileName, PhysicalPath, Size, AddedAt, MetadataJson)
                VALUES ($id, $customerId, $fileName, $path, $size, $addedAt, $metadata)
                ON CONFLICT(Id) DO UPDATE SET
                    FileName=$fileName,
                    PhysicalPath=$path,
                    Size=$size,
                    AddedAt=$addedAt,
                    MetadataJson=$metadata;";

            command.Parameters.AddWithValue("$id", attachment.Id);
            command.Parameters.AddWithValue("$customerId", id.Value);
            command.Parameters.AddWithValue("$fileName", attachment.FileName);
            command.Parameters.AddWithValue("$path", attachment.PhysicalPath);
            command.Parameters.AddWithValue("$size", attachment.Size);
            command.Parameters.AddWithValue("$addedAt", attachment.AddedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$metadata", JsonSerializer.Serialize(new { attachment.Size }));

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    private static Task<Unit> DeleteCustomerInternal(DbConnection connection, DbTransaction transaction, CustomerId id, CancellationToken cancellationToken)
    {
        var sqliteConnection = (SqliteConnection)connection;
        var sqliteTransaction = (SqliteTransaction)transaction;
        var command = sqliteConnection.CreateCommand();
        command.Transaction = sqliteTransaction;
        command.CommandText = "DELETE FROM Customers WHERE Id=$id;";
        command.Parameters.AddWithValue("$id", id.Value);
        return ExecuteNonQueryAsync(command, cancellationToken);
    }

    private static async Task<Unit> ExecuteNonQueryAsync(DbConnection connection, DbTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        var sqliteConnection = (SqliteConnection)connection;
        var sqliteTransaction = (SqliteTransaction)transaction;
        var command = sqliteConnection.CreateCommand();
        command.Transaction = sqliteTransaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    private static async Task<Unit> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    private static async Task<ImmutableArray<CustomerAttachment>> LoadAttachmentsAsync(SqliteConnection connection, CustomerId id, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, FileName, PhysicalPath, Size, AddedAt
            FROM Attachments
            WHERE CustomerId = $customerId;";
        command.Parameters.AddWithValue("$customerId", id.Value);

        var builder = ImmutableArray.CreateBuilder<CustomerAttachment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            builder.Add(MapAttachment(reader));
        }

        return builder.ToImmutable();
    }

    private static CustomerAttachment MapAttachment(SqliteDataReader reader)
    {
        var attachmentId = reader.GetGuid(0);
        var fileName = reader.GetString(1);
        var physicalPath = reader.GetString(2);
        var size = reader.GetInt64(3);
        var addedAtRaw = reader.GetString(4);
        var addedAt = DateTimeOffset.TryParse(addedAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

        return new CustomerAttachment(attachmentId, fileName, physicalPath, size, addedAt);
    }

    private static class Schema
    {
        public const string CreateCustomers = @"
            CREATE TABLE IF NOT EXISTS Customers (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                SecondaryEmail TEXT,
                Phone TEXT NOT NULL,
                AddressLine1 TEXT,
                AddressLine2 TEXT,
                City TEXT,
                Country TEXT,
                PostalCode TEXT,
                PreferredChannel TEXT,
                ScoreNumerator REAL NOT NULL,
                ScoreDenominator REAL NOT NULL,
                TrendReal REAL NOT NULL,
                TrendImaginary REAL NOT NULL,
                IsArchived INTEGER NOT NULL DEFAULT 0
            );";

        public const string CreateActivities = @"
            CREATE TABLE IF NOT EXISTS Activities (
                Id TEXT PRIMARY KEY,
                CustomerId TEXT NOT NULL REFERENCES Customers(Id) ON DELETE CASCADE,
                Kind TEXT NOT NULL,
                OccurredAt TEXT NOT NULL,
                Summary TEXT NOT NULL,
                DurationSeconds REAL
            );";

        public const string CreateAttachments = @"
            CREATE TABLE IF NOT EXISTS Attachments (
                Id TEXT PRIMARY KEY,
                CustomerId TEXT NOT NULL REFERENCES Customers(Id) ON DELETE CASCADE,
                FileName TEXT NOT NULL,
                PhysicalPath TEXT NOT NULL,
                Size INTEGER NOT NULL,
                AddedAt TEXT NOT NULL,
                MetadataJson TEXT
            );";
    }
}
