﻿using System;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Quermine
{
    public abstract class DbClient : IDisposable
	{
		internal abstract Task OpenAsync();

		internal abstract QueryBuilder Builder { get; }

		/// <summary>
		/// The current state of the underlying connection of this client.
		/// </summary>
		public abstract ConnectionState State { get; }

		public abstract void Dispose();

		public abstract Task<ResultSet> Execute(Query query);

		public async Task<ResultSet> Execute(string commandString)
		{
			Query query = new Query(Builder, commandString);
			return await Execute(query);
		}

		public abstract Task<NonQueryResult> ExecuteNonQuery(Query query);

		public async Task<NonQueryResult> ExecuteNonQuery(string commandString)
		{
			Query query = new Query(Builder, commandString);
			return await ExecuteNonQuery(query);
		}

		public abstract Task<object> ExecuteScalar(Query query);

		public async Task<object> ExecuteScalar(string commandString)
		{
			Query query = new Query(Builder, commandString);
			return await ExecuteScalar(query);
		}

		public async Task<T> ExecuteScalar<T>(Query query)
		{
			return (T)(await ExecuteScalar(query));
		}

		public async Task<T> ExecuteScalar<T>(string commandString)
		{
			Query query = new Query(Builder, commandString);
			return (T)(await ExecuteScalar(query));
		}

		public virtual async Task<List<T>> Execute<T>(SelectQuery<T> query) where T : new()
		{
			ResultSet result = await Execute(query as Query);

			List<T> resultObjects = new List<T>();

			foreach (ResultRow row in result)
			{
				resultObjects.Add(await query.Deserialize(this, row));
			}

			return resultObjects;
		}

		public abstract Task<List<NonQueryResult>> ExecuteTransaction(IsolationLevel isolationLevel, params Query[] queries);

		public abstract Task<TableSchema> GetTableSchema(string table);

		public abstract Task<List<string>> GetTableNames();

		public Task ExecuteTransaction(params Query[] queries)
		{
			return ExecuteTransaction(IsolationLevel.Unspecified, queries);
		}

		/// <summary>
		/// Insert the given serializable object into the DB
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <returns>The number of rows affected</returns>
		public Task<NonQueryResult> Insert<T>(T obj)
		{
			InsertQuery<T> query = new InsertQuery<T>(Builder, obj);
			return ExecuteNonQuery(query);
		}

		public Task<List<T>> Select<T>() where T : new()
		{
			SelectQuery<T> query = new SelectQuery<T>(Builder);
			return Execute(query);
		}

		/// <summary>
		/// Delete any rows from the DB that match the values of the given object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <returns>The number of rows affected</returns>
		public Task<NonQueryResult> Delete<T>(T obj)
		{
			DeleteQuery<T> query = new DeleteQuery<T>(Builder, obj);
			return ExecuteNonQuery(query);
		}

		/// <summary>
		/// This method will update the object using the given method, and will also apply these updates to the database row of this object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="updateFunc"></param>
		/// <returns></returns>
		public Task<NonQueryResult> Update<T>(T obj, Func<T, T> updateFunc) where T : new()
		{
			UpdateQuery<T> query = new UpdateQuery<T>(Builder);

			query.Where(obj);

			obj = updateFunc(obj);

			query.Set(obj);

			return ExecuteNonQuery(query);
		}

		/// <summary>
		/// This method will update the object using the given method, and will also apply these updates to the database row of this object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="updateFunc"></param>
		/// <returns></returns>
		public Task<NonQueryResult> Update<T>(T obj, Action<T> updateFunc) where T : new()
		{
			return Update<T>(obj, o =>
			{
				updateFunc(o);
				return o;
			});
		}
	}
}
