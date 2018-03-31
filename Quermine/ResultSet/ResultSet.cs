﻿using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Quermine
{
    /// <summary>
    /// Holds the schema and data of a query result.
    /// Implements ICollection.
    /// </summary>
    public class ResultSet : IEnumerable<ResultRow>
    {
        Dictionary<string, Type> schema;
        List<ResultRow> rows;
        
        public int RowCount
        {
            get { return rows.Count; }
        }        

        internal ResultSet(DbDataReader reader)
        {
            schema = new Dictionary<string, Type>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                Type type = reader.GetFieldType(i);

                schema.Add(
                    reader.GetName(i),
                    reader.GetFieldType(i)
                    );
            }
            
            rows = new List<ResultRow>();
        }

        internal ResultRow AddRow(DbDataReader row)
        {
            ResultRow dbRow = new ResultRow(schema, row);
            rows.Add(dbRow);
            return dbRow;
        }

		internal static async Task<ResultSet> FromReader(DbDataReader reader)
		{
			ResultSet rs = new ResultSet(reader);

			while (await reader.ReadAsync())
			{
				ResultRow row = rs.AddRow(reader);
			}

			return rs;
		}

        public IEnumerator<ResultRow> GetEnumerator()
        {
            return ((IEnumerable<ResultRow>)rows).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ResultRow>)rows).GetEnumerator();
        }
    }
}