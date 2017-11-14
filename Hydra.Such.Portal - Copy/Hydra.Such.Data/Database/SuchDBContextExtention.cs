﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Text;

namespace Hydra.Such.Data.Database
{
    public class SuchDBContextExtention : SuchDBContext
    {
        public virtual IEnumerable<dynamic> execStoredProcedure(String cmdText, SqlParameter[] parameters)
        {

            using (var connection = new SqlConnection("data source=10.101.1.10\\SQLNAVDEV;initial catalog=PlataformaOperacionalSUCH;user id=such_portal_user;password=SuchPW.2K17;"))
            {
                connection.Open();

                using (var command = new SqlCommand(cmdText, connection))
                {
                    foreach (SqlParameter item in parameters)
                    {
                        command.Parameters.Add(item.ParameterName, System.Data.SqlDbType.NVarChar);
                        command.Parameters[item.ParameterName].Value = item.Value == null ? "" : item.Value;
                    }

                    using (var dataReader = command.ExecuteReader())
                    {
                        var fields = new List<String>();

                        for (var i = 0; i < dataReader.FieldCount; i++)
                        {
                            fields.Add(dataReader.GetName(i));
                        }

                        while (dataReader.Read())
                        {
                            var item = new ExpandoObject() as IDictionary<String, Object>;

                            for (var i = 0; i < fields.Count; i++)
                            {
                                item.Add(fields[i], dataReader[fields[i]]);
                            }

                            yield return item;
                        }
                    }
                }
            }
        }
    }
}