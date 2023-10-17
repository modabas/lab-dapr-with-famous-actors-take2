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
    public static IDbContextTransaction BeginTransaction(this DatabaseFacade database, IOutboxPersistor? outboxPersistor = null)
    {
        var transaction = database.BeginTransaction();
        if (outboxPersistor is not null)
            outboxPersistor.DbTransaction = transaction.GetDbTransaction();
        return transaction;
    }

    public static IDbContextTransaction BeginTransaction(this DatabaseFacade database, IsolationLevel isolationLevel, IOutboxPersistor? outboxPersistor = null)
    {
        var transaction = database.BeginTransaction(isolationLevel);
        if (outboxPersistor is not null)
            outboxPersistor.DbTransaction = transaction.GetDbTransaction();
        return transaction;
    }
}
