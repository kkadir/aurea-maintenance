﻿namespace Aurea.Maintenance.Debugger.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Aurea.Logging;
    using Aurea.Maintenance.Debugger.Common;
    using Aurea.Maintenance.Debugger.Common.Models;
    using CIS.Framework.Data;
    using CIS.Framework.Security;

    public static class ICopyableExtension
    {
        private static int _entityId;
        private static string _connectionString;
        private static string _dbPrefix;
        private static bool _dryRun;
        private static ILogger _logger;
        private static List<string> _processedEntities = new List<string>();
        private static List<string> _constrainDisabledEntities = new List<string>();
        private static Dictionary<string, string> _fieldNamesCache = new Dictionary<string, string>();

		public static bool CopyEntityFromSaes2Daes<T>(this T entity, string connectionString, string dbPrefix, ILogger logger, bool dryRun) where T :  ICopyableEntity
		{
		    _connectionString = connectionString;
		    _dbPrefix = dbPrefix;
		    _dryRun = dryRun;
		    _logger = logger;
		    _processedEntities.Clear();
		    _constrainDisabledEntities.Clear();
		    _fieldNamesCache.Clear();
            _logger.Info($"Starting copying of type {entity.ToString()}");

            using (var ts = TransactionFactory.CreateTransactionScope())
			{
			    var tableAttribute = entity.GetType().GetCustomAttributesIncludingBaseInterfaces<TableAttribute>().First();
                try
			    {
			        _entityId = (int)entity.GetPropertyValue(tableAttribute.PrimaryKey, true);
                    var relatedAttributes = entity.GetType().GetCustomAttributesIncludingBaseInterfaces<RelatedEntityAttribute>();

			        if (CopyChildEntity(entity.GetType(), _entityId, tableAttribute, relatedAttributes))
			        {
                        _constrainDisabledEntities.ForEach(entityName =>
                        {
                            SqlHelper.ExecuteNonQuery(_connectionString, CommandType.Text,
                                $"ALTER TABLE [daes_{_dbPrefix}].[{tableAttribute.TableSchema}].[{tableAttribute.TableName}] WITH CHECK CHECK CONSTRAINT ALL");
                        });

			            ts.Complete();
			            return true;
			        }
			        return false;
			    }
			    catch (Exception e)
			    {
			        _logger.Error(e, $"Error occurred while copying data of {tableAttribute.TableName}");
                    return false;
			    }
			}
		}

		private static bool CopyChildEntity(Type entity, int entityId, TableAttribute tableAttribute, IEnumerable<RelatedEntityAttribute> relatedAttributes)
		{
			_logger.Info($"Start copying entity {tableAttribute.TableName}");
		    if (_processedEntities.Contains($"{entity.FullName}_{entityId}"))
		    {
		        return true;
		    }

		    

            //foreach (var relatedAttribute in relatedAttributes.Where(x => x.IsRequiredBeforeCopy && !x.RelatedEntity.Equals(entity)))
			//{
			//	var childTableAttribute = relatedAttribute.RelatedEntity.GetCustomAttributesIncludingBaseInterfaces<TableAttribute>().First();
			//	var childRelatedAttributes = relatedAttribute.RelatedEntity.GetCustomAttributesIncludingBaseInterfaces<RelatedEntityAttribute>();
			//	if (!CopyChildEntity(relatedAttribute.RelatedEntity, entityId, childTableAttribute, childRelatedAttributes))
			//	{
			//		return false;
			//	}
			//}

			var sql = ConstructCopySql(entity, tableAttribute, relatedAttributes.First(), entityId);
			try
			{
				if (!_dryRun)
				{
					SqlHelper.ExecuteNonQuery(SqlHelper.CreateCommand(_connectionString, sql, CommandType.Text));
				}

                _processedEntities.Add($"{entity.FullName}_{entityId}");

			    foreach (var relatedAttribute in relatedAttributes.OrderByDescending(x => x.Sequence)) 
				{
					var childTableAttribute = relatedAttribute.RelatedEntity.GetCustomAttributesIncludingBaseInterfaces<TableAttribute>().First();
					var childRelatedAttributes = relatedAttribute.RelatedEntity.GetCustomAttributesIncludingBaseInterfaces<RelatedEntityAttribute>();
					if (!CopyChildEntity(relatedAttribute.RelatedEntity, entityId, childTableAttribute, childRelatedAttributes))
					{
						return false;
					}
				}

				return true;
			}
			catch (Exception e)
			{
				_logger.Error(e, $"Error occurred while copying data of {tableAttribute.TableName}");
				return false;
			}
		}

		private static string ConstructCopySql(Type entity, TableAttribute tableAttribute, RelatedEntityAttribute relatedAttribute, int keyValue)
		{
		    var fieldNames = string.Empty;
		    _fieldNamesCache.TryGetValue(entity.FullName, out fieldNames);

		    if (string.IsNullOrEmpty(fieldNames))
		    {
		        var fields = entity.GetProperties();
		        fieldNames = string.Join(", ", fields.Select(x => $"[{x.Name}]").ToArray());
		        _fieldNamesCache.Add(entity.FullName, fieldNames);

            }

		    var sql = new StringBuilder();
		    if (!_constrainDisabledEntities.Contains(entity.FullName))
		    {
		        sql.AppendLine($"ALTER TABLE [daes_{_dbPrefix}].[{tableAttribute.TableSchema}].[{tableAttribute.TableName}] NOCHECK CONSTRAINT ALL");
		        _constrainDisabledEntities.Add(entity.FullName);
		    }

		    if (tableAttribute.HasIdentity)
			{
				sql.AppendLine($"SET IDENTITY_INSERT [daes_{_dbPrefix}].[{tableAttribute.TableSchema}].[{tableAttribute.TableName}] ON ");
			}

			sql.AppendLine($@"INSERT INTO [daes_{_dbPrefix}].[{tableAttribute.TableSchema}].[{tableAttribute.TableName}] ({fieldNames}) ");
		    sql.AppendLine($@"SELECT {fieldNames} FROM [saes_{_dbPrefix}].[{tableAttribute.TableSchema}].[{tableAttribute.TableName}] src ");
		    sql.AppendLine($@"WHERE {tableAttribute.PrimaryKey} = {keyValue}  ");
		    sql.AppendLine($@"AND NOT EXISTS(SELECT 1 FROM [daes_{_dbPrefix}].[{tableAttribute.TableSchema}].[{tableAttribute.TableName}] dst WHERE dst.{tableAttribute.PrimaryKey} = src.{tableAttribute.PrimaryKey})");

            if (tableAttribute.HasIdentity)
		    {
		        sql.AppendLine($"SET IDENTITY_INSERT [daes_{_dbPrefix}].[{tableAttribute.TableSchema}].[{tableAttribute.TableName}] OFF ");
		    }

			return sql.ToString();
		}

	}
}
