﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;

namespace Quermine
{
	public class SelectQuery<T> : SelectQuery where T : new()
	{
		internal SelectQuery(QueryBuilder builder) : base(builder)
		{
			// Get table name
			DbTable tableAttribute = typeof(T)
				.GetCustomAttributes<DbTable>(true)
				.FirstOrDefault();

			if (tableAttribute != null)
			{
				From(tableAttribute.Name);
			}
			else
			{
				From(typeof(T).Name);
			}
			
		}

		internal async Task<T> Deserialize(DbClient conn, ResultRow row)
		{
			T obj = new T();

			// Get custom fields
			FieldInfo[] fields = obj.GetType().GetFields();
			foreach (FieldInfo field in fields)
			{
				object value = await GetMemberValue(field, conn, row);

				if (value != null)
				{
					field.SetValue(obj, value);
				}
			}

			// Get custom properties
			PropertyInfo[] properties = obj.GetType().GetProperties();
			foreach (PropertyInfo property in properties)
			{
				object value = await GetMemberValue(property, conn, row);

				if (value != null && property.GetSetMethod() != null)
				{
					property.SetValue(obj, value);
				}
			}

			return obj;
		}

		async Task<object> GetMemberValue(MemberInfo member, DbClient conn, ResultRow row)
		{
			DbField columnAttribute = member.GetCustomAttribute<DbField>(true);
			DbReference referenceAttribute = member.GetCustomAttribute<DbReference>(true);

			if (columnAttribute != null)
			{
				object value = row[columnAttribute.Name ?? member.Name];

				if (!(value is DBNull))
				{
					return value;
				}
			}
			if (referenceAttribute != null)
			{
				object value = row[referenceAttribute.Column];

				ReferenceType refType = ReferenceType.Singular;
				Type type = (member is PropertyInfo) ? (member as PropertyInfo).PropertyType : (member as FieldInfo).FieldType;
				
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
				{
					type = type.GetGenericArguments()[0];
					refType = ReferenceType.List;
				}
				else if (type.IsArray)
				{
					type = type.GetElementType();
					refType = ReferenceType.Array;
				}

				object result = await (Task<object>)GetNestedMethod().MakeGenericMethod(type).Invoke(
					this,
					new object[]
					{
						conn,
						referenceAttribute.ForeignColumn,
						value,
						refType
					}
				);

				if (!(value is DBNull))
				{
					return result;
				}
			}
			return null;
		}

		MethodInfo GetNestedMethod()
		{
			MethodInfo method = GetType().GetMethod("DeserializeNested", 
				BindingFlags.Instance | BindingFlags.NonPublic,
				null,
				new Type[] {
					typeof(DbClient), typeof(string), typeof(object), typeof(ReferenceType)
				},
				null);
			return method;
		}

		async Task<object> DeserializeNested<R>(DbClient conn, string column, object value, ReferenceType refType) where R : new()
		{
			SelectQuery<R> query = new SelectQuery<R>(builder);
			query.Where(column, value);
			List<R> result = await conn.Execute(query);

			switch (refType)
			{
				case ReferenceType.Singular:
					return result.FirstOrDefault();

				case ReferenceType.List:
					return result;

				case ReferenceType.Array:
					return result.ToArray();

				default:
					goto case ReferenceType.Singular;
			}
		}

		enum ReferenceType
		{
			Singular,
			List,
			Array
		}
	}
}