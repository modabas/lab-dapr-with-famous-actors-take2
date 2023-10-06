using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using PublisherService.Core.Database.OutboxPattern.Service;
using System.Data;

namespace PublisherService.Infrastructure.Database.Postgres.EntityFw.Extensions;

public static class DatabaseFacadeExtensions
{
    //!!!IMPORTANT
    //if we override BeginTransactionAsync and implement with async keyword,
    //AsyncLocal parameter DbTransaction of calling repo will be lost when async method will return to caller
    //we have to stay in current async context or in its children
    //so we only use sync BeginTransaction overloads to set DbTransaction
    public static IDbContextTransaction BeginTransaction(this DatabaseFacade database, IOutboxWriter? outboxPublisher = null)
    {
        var transaction = database.BeginTransaction();
        if (outboxPublisher is not null)
            outboxPublisher.DbTransaction = transaction.GetDbTransaction();
        return transaction;
    }

    public static IDbContextTransaction BeginTransaction(this DatabaseFacade database, IsolationLevel isolationLevel, IOutboxWriter? outboxPublisher = null)
    {
        var transaction = database.BeginTransaction(isolationLevel);
        if (outboxPublisher is not null)
            outboxPublisher.DbTransaction = transaction.GetDbTransaction();
        return transaction;
    }
}
