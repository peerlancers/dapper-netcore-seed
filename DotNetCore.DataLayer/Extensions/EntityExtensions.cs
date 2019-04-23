﻿using DotNetCore.DataLayer.Attributes;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using static DotNetCore.DataLayer.Extensions.StringExtensions;

namespace DotNetCore.DataLayer.Extensions
{
    public static class EntityExtensions
    {
        public static string GetTableName(this IEntity entity, CaseType caseType = CaseType.SnakeCase)
        {
            Type entityType = entity.GetType();
            bool hasSpecifiedTableName = Attribute.IsDefined(entityType, typeof(DbTableNameAttribute));
            var tableName = $"{entityType.Name.PascalCaseTo(caseType)}s";

            if (hasSpecifiedTableName)
            {
                if (entityType.GetCustomAttributes(typeof(DbTableNameAttribute), true).FirstOrDefault() is DbTableNameAttribute attribute)
                {
                    tableName = attribute.TableName;
                }
            }
            
            return tableName;
        }

        public static (string sql, object param) ToInsertData(this IEntity entity, bool useSnakeCase = false)
        {
            var fields = new StringBuilder();
            var param = new StringBuilder();
            var paramObject = (IDictionary<string, object>)new ExpandoObject();

            Type entityType = entity.GetType();
            var delimiter = "";

            foreach (PropertyInfo propertyInfo in entityType.GetProperties())
            {
                if (propertyInfo.IsValidSqlField())
                {
                    string fieldName = GetFieldName(propertyInfo, useSnakeCase);
                    fields.Append($"{delimiter}{fieldName}");
                    param.Append($"{delimiter}@{fieldName}");
                    delimiter = ", ";

                    paramObject.Add(fieldName, propertyInfo.GetValue(entity));
                }
            }

            string sql = $"INSERT INTO {entity.GetTableName()} ({fields.ToString()}) VALUES({param.ToString()});";

            return (sql, paramObject);
        }

        public static (string sql, object param) ToUpdateData(this IEntity entity, bool useSnakeCase = false)
        {
            var fields = new StringBuilder();
            var paramObject = (IDictionary<string, object>)new ExpandoObject();

            Type entityType = entity.GetType();
            var delimiter = "";
            foreach (PropertyInfo propertyInfo in entityType.GetProperties())
            {
                if (propertyInfo.IsValidSqlField())
                {
                    string fieldName = GetFieldName(propertyInfo, useSnakeCase);
                    fields.Append($"{delimiter}{fieldName} = @{fieldName}");
                    delimiter = ", ";
                    paramObject.Add(fieldName, propertyInfo.GetValue(entity));
                }
            }

            string idParam = useSnakeCase ? nameof(entity.Id).ToSnakeCase() : nameof(entity.Id);
            string sql = $"UPDATE {entity.GetTableName()} SET {fields.ToString()} WHERE id = @{idParam};";

            return (sql, paramObject);
        }

        private static string GetFieldName(PropertyInfo propertyInfo, bool useSnakeCase = false)
        {
            bool hasSpecifiedFieldName = Attribute.IsDefined(propertyInfo, typeof(DbFieldNameAttribute));

            var fieldName = useSnakeCase ? propertyInfo.Name.ToSnakeCase() : propertyInfo.Name;
            if (hasSpecifiedFieldName)
            {
                if (propertyInfo.GetCustomAttributes(typeof(DbFieldNameAttribute), true).FirstOrDefault() is DbFieldNameAttribute attribute)
                {
                    fieldName = attribute.FieldName;
                }
            }

            return fieldName;
        }
    }
}
