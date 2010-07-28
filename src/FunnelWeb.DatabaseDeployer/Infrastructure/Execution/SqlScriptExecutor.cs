﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using FunnelWeb.DatabaseDeployer.Infrastructure.ScriptProviders;

namespace FunnelWeb.DatabaseDeployer.Infrastructure.Execution
{
    /// <summary>
    /// A standard implementation of the IScriptExecutor interface that executes against a SQL Server 
    /// database using SQL Server SMO.
    /// </summary>
    public sealed class SqlScriptExecutor : IScriptExecutor
    {
        private static string[] SplitByGoStatements(string script)
        {
            var scriptStatements = Regex.Split(script, "^\\s*GO\\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)
                                       .Select(x => x.Trim())
                                       .Where(x => x.Length > 0)
                                       .ToArray();
            return scriptStatements;
        }

        /// <summary>
        /// Executes the specified script against a database at a given connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="script">The script.</param>
        public void Execute(string connectionString, IScript script, ILog log)
        {
            log.WriteInformation("Executing SQL Server script '{0}'", script.Name);
            var connection = new SqlConnection(connectionString);
            var scriptStatements = SplitByGoStatements(script.Contents);
            var index = -1;
            try
            {
                using (connection)
                {
                    connection.Open();

                    var transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);

                    foreach (var statement in scriptStatements)
                    {
                        index++;
                        var command = new SqlCommand(statement, connection, transaction);
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
            catch (SqlException sqlException)
            {
                log.WriteInformation("SQL exception has occured. Transaction rolled back for script: '{0}'", script.Name);
                log.WriteError("Script block number: {0}; Block line {1}; Message: {2}", index, sqlException.LineNumber, sqlException.Procedure, sqlException.Number, sqlException.Message);
                log.WriteError(sqlException.ToString());
                throw;
            }
            catch (Exception ex)
            {
                log.WriteInformation("Exception has occured. Transaction rolled back for script: '{0}'", script.Name);
                log.WriteError(ex.ToString());
                throw;
            }
        }
    }
}