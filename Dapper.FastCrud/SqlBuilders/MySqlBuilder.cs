﻿namespace Dapper.FastCrud.SqlBuilders
{
    using System;
    using System.Linq;
    using Dapper.FastCrud.EntityDescriptors;
    using Dapper.FastCrud.Mappings;

    internal class MySqlBuilder:GenericStatementSqlBuilder
    {
        public MySqlBuilder(EntityDescriptor entityDescriptor, EntityMapping entityMapping)
            : base(entityDescriptor, entityMapping, SqlDialect.MySql)
        {
        }

        /// <summary>
        /// Constructs a full insert statement
        /// </summary>
        protected override string ConstructFullInsertStatementInternal()
        {
            var sql = this.ResolveWithCultureInvariantFormatter(
                    $"INSERT INTO {this.GetTableName()} ({this.ConstructColumnEnumerationForInsert()}) VALUES ({this.ConstructParamEnumerationForInsert()}); ");

            if (this.RefreshOnInsertProperties.Length > 0)
            {
                // we have to bring some column values back
                if (this.KeyProperties.Length == 0)
                {
                    throw new NotSupportedException($"Entity '{this.EntityMapping.EntityType.Name}' has database generated fields but no primary key to retrieve them with after insertion.");
                }


                // we have an identity column, so we can fetch the rest of them
                if (this.InsertKeyDatabaseGeneratedProperties.Length == 1 && this.RefreshOnInsertProperties.Length == 1)
                {
                    // just one, this is going to be easy
                    sql += this.ResolveWithCultureInvariantFormatter($"SELECT LAST_INSERT_ID() as {this.GetDelimitedIdentifier(this.InsertKeyDatabaseGeneratedProperties[0].PropertyName)}");
                }
                else 
                {
                    // There are no primary keys generated by the database
                    if (this.InsertKeyDatabaseGeneratedProperties.Length == 0)
                    {
                        sql += $"SELECT {this.ConstructRefreshOnInsertColumnSelection()} FROM {this.GetTableName()} WHERE" + this.ConstructKeysWhereClause();
                    }
                    else
                    {
                        sql += this.ResolveWithCultureInvariantFormatter($"SELECT {this.ConstructRefreshOnInsertColumnSelection()} FROM {this.GetTableName()} WHERE {this.GetColumnName(this.InsertKeyDatabaseGeneratedProperties[0], null, false)} = LAST_INSERT_ID()");
                    }
                }
            }
            return sql;
        }

        protected override string ConstructFullSelectStatementInternal(
            string selectClause,
            string fromClause,
            FormattableString whereClause = null,
            FormattableString orderClause = null,
            long? skipRowsCount = null,
            long? limitRowsCount = null,
            bool forceTableColumnResolution = false)
        {
            var sql = this.ResolveWithCultureInvariantFormatter($"SELECT {selectClause} FROM {fromClause}");

            if (whereClause != null)
            {
                sql += " WHERE " + this.ResolveWithSqlFormatter(whereClause, forceTableColumnResolution);
            }
            if (orderClause != null)
            {
                sql += " ORDER BY " + this.ResolveWithSqlFormatter(orderClause, forceTableColumnResolution);
            }

            if (skipRowsCount.HasValue)
            {
                sql += this.ResolveWithCultureInvariantFormatter($" LIMIT {skipRowsCount},{limitRowsCount ?? (int?)int.MaxValue}");
            }
            else if (limitRowsCount.HasValue)
            {
                sql += this.ResolveWithCultureInvariantFormatter($" LIMIT {limitRowsCount}");
            }

            return sql;
        }
    }
}
