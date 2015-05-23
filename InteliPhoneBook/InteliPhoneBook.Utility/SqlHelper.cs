using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Web;
//using System.Web.Security;
using System.Data;
using System.Data.SqlClient;
using log4net;
using System.Configuration;

namespace IPOMorningCallServiceApp
{
    /// <summary>
    /// 数据库操作基类
    /// </summary>
    public abstract class SqlHelper
    {
        public static log4net.ILog log = log4net.LogManager.GetLogger("sqlhelper");
        /// <summary>
        /// 连接字符串
        /// </summary>
        ///
        public static readonly string SqlconnString = "Data Source=" + ConfigurationManager.AppSettings["DBIP"] + ";Initial Catalog=" + ConfigurationManager.AppSettings["DBName"] + ";User ID =" + ConfigurationManager.AppSettings["DBUser"] + ";pwd=" + ConfigurationManager.AppSettings["DBPass"] + ";Max Pool Size = 512;";
        // 哈希表用来存储缓存的参数信息，哈希表可以存储任意类型的参数。
        private static Hashtable parmCache = Hashtable.Synchronized(new Hashtable());

        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)//判断数据库连接状态
            {
                conn.Open();
            }
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)//判断是否需要事物处理
            {
                cmd.Transaction = trans;
            }
            cmd.CommandType = cmdType;
            if (cmdParms != null)
            {
                foreach (SqlParameter parm in cmdParms)
                {
                    cmd.Parameters.Add(parm);
                }
            }
        }

        public static void CacheParameters(string cacheKey, params SqlParameter[] commandParameters)
        {
            parmCache[cacheKey] = commandParameters;
        }

        public static SqlParameter[] GetCachedParameters(string cacheKey)
        {
            SqlParameter[] cachedParms = (SqlParameter[])parmCache[cacheKey];

            if (cachedParms == null)
                return null;

            //新建一个参数的克隆列表
            SqlParameter[] clonedParms = new SqlParameter[cachedParms.Length];

            //通过循环为克隆参数列表赋值
            for (int i = 0, j = cachedParms.Length; i < j; i++)
                //使用clone方法复制参数列表中的参数
                clonedParms[i] = (SqlParameter)((ICloneable)cachedParms[i]).Clone();

            return clonedParms;
        }


        /// <summary>
        /// 用于数据库Update,Insert,Delete等操作，成功返回大于0
        /// </summary>
        public static int ExecuteNonQuery(CommandType cmdType, string cmdText, params SqlParameter[] cmdParams)
        {
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection conn = new SqlConnection(SqlconnString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParams);//通过PrePareCommand方法将参数逐个加入到SqlCommand的参数集合中
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();//清空SqlCommand中的参数列表
                conn.Close();
                return val;
            }
        }


        /// <summary>
        /// 用于数据库Update,Insert,Delete等操作，成功返回大于0
        /// </summary>
        public static int ExecuteNonQuery(string cmdText, out int rel, params SqlParameter[] cmdParams)
        {
            rel = 0;
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection conn = new SqlConnection(SqlconnString))
            {
                try
                {
                    PrepareCommand(cmd, conn, null, CommandType.Text, cmdText, cmdParams);//通过PrePareCommand方法将参数逐个加入到SqlCommand的参数集合中
                    SqlParameter para = new SqlParameter("@return", rel);
                    para.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(para);
                    cmd.ExecuteNonQuery();
                    if (para.Value.ToString() != "")
                        rel = int.Parse(para.Value.ToString());
                    else
                        rel = 0;
                    cmd.Parameters.Clear();//清空SqlCommand中的参数列表
                    conn.Close();
                    return rel;
                }
                catch
                {
                    rel = 0;
                    conn.Close();
                    throw;
                }
            }
        }


        /// <summary>
        /// 用于查询数据库，返回单个对象Object
        /// </summary>
        public static object ExecuteScalar(CommandType cmdType, string cmdText, params SqlParameter[] cmdParams)
        {
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection conn = new SqlConnection(SqlconnString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParams);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                conn.Close();
                return val;
            }
        }


        /// <summary>
        /// 用于查询数据库，返回单个对象Object
        /// </summary>
        public static object ExecuteScalar(SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] cmdParams)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, cmdParams);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;

        }

        /// <summary>
        /// 用于查询数据库，返回SqlDataReader
        /// </summary>
        public static SqlDataReader ExecuteReader(CommandType cmdType, string cmdText, params SqlParameter[] cmdParams)
        {
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn = new SqlConnection(SqlconnString);
            // 在这里使用try/catch处理是因为如果方法出现异常，则SqlDataReader就不存在，
            //CommandBehavior.CloseConnection的语句就不会执行，触发的异常由catch捕获。
            //关闭数据库连接，并通过throw再次引发捕捉到的异常。
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParams);
                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return dr;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }


        /// <summary>
        /// 用于查询数据库，返回DataTable
        /// </summary>
        public static DataTable ExecuteReader(string cmdText, string table)
        {
            SqlConnection conn = new SqlConnection(SqlconnString);
            // 在这里使用try/catch处理是因为如果方法出现异常，则SqlDataReader就不存在，
            //CommandBehavior.CloseConnection的语句就不会执行，触发的异常由catch捕获。
            //关闭数据库连接，并通过throw再次引发捕捉到的异常。
            try
            {
                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmdText, conn);
                da.Fill(ds, table);
                return ds.Tables[table];
            }
            finally
            {
                conn.Close();
            }
        }



        /// <summary>
        /// 执行一个程储过程或T-SQL语句,返回一个DataTable
        /// </summary>
        /// <param name="connectionString">现有的数据库连接</param>
        /// <param name="cmdType">执行类型</param>
        /// <param name="cmdText">存储过程的名称或T-SQL语句</param>
        /// <param name="commandParameters">存储过程参数集合</param>
        /// <returns>返回一个DataTable</returns>
        public static DataTable ExecuteDataTable(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();

            using (SqlConnection conn = new SqlConnection(SqlconnString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                //da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                DataTable dt = new DataTable();
                da.Fill(dt);
                cmd.Parameters.Clear();
                return dt;
            }
        }


        /// <summary>
        /// 用于查询数据库，返回从StartIndex起的PageSize条数据的DataTable
        /// </summary>
        public static DataTable ExecuteReader(string cmdText, int StartIndex, int PageSize, string table)
        {
            SqlConnection conn = new SqlConnection(SqlconnString);
            // 在这里使用try/catch处理是因为如果方法出现异常，则SqlDataReader就不存在，
            //CommandBehavior.CloseConnection的语句就不会执行，触发的异常由catch捕获。
            //关闭数据库连接，并通过throw再次引发捕捉到的异常。
            try
            {
                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmdText, conn);
                da.Fill(ds, StartIndex, PageSize, table);
                return ds.Tables[table];
            }
            finally
            {
                conn.Close();
            }

        }


        /// <summary>
        /// 用于查询数据库，返回DataTable
        /// </summary>
        public static DataTable ExecuteReader(string cmdText, string table, params SqlParameter[] cmdParams)
        {
            SqlConnection conn = new SqlConnection(SqlconnString);
            // 在这里使用try/catch处理是因为如果方法出现异常，则SqlDataReader就不存在，
            //CommandBehavior.CloseConnection的语句就不会执行，触发的异常由catch捕获。
            //关闭数据库连接，并通过throw再次引发捕捉到的异常。
            try
            {
                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmdText, conn);
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                if (cmdParams != null)
                {
                    foreach (SqlParameter parm in cmdParams)
                    {
                        da.SelectCommand.Parameters.Add(parm);
                    }
                }
                da.Fill(ds, table);
                return ds.Tables[table];
            }
            finally
            {
                conn.Close();
            }
        }


        /// <summary>
        /// Execute a SqlCommand (that returns no resultset) using an existing SQL Transaction 来自PetShop
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="trans">an existing sql transaction</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>an int representing the number of rows affected by the command</returns>
        public static int ExecuteNonQuery(SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters);
            int val = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return val;
        }




        /// <summary>
        /// Execute a SqlCommand (that returns no resultset) using an existing SQL Transaction 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  bool bSucceed = ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a OleDbConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>是否执行成功</returns>
        public static bool ExecuteNonQueryByTran(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        SqlCommand cmd = new SqlCommand();
                        PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters);
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                        //return false;
                    }
                }
            }
        }

        /// <summary>
        /// Execute a SqlCommand that returns a resultset against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  SqlDataReader r = ExecuteReader(connString, cmd, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a OleDbConnection</param>
        /// <param name="cmd">T-SQL command</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>A SqlDataReader containing the results</returns>
        public static SqlDataReader ExecuteReader(string connectionString, SqlCommand cmd, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlConnection conn = new SqlConnection(connectionString);

            // we use a try/catch here because if the method throws an exception we want to 
            // close the connection throw code, because no datareader will exist, hence the 
            // commandBehaviour.CloseConnection will not work
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }

        /// <summary>
        /// Execute a SqlCommand that returns a resultset against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  SqlDataReader r = ExecuteReader(connString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a OleDbConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>A SqlDataReader containing the results</returns>
        public static SqlDataReader ExecuteReader(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn = new SqlConnection(connectionString);

            // we use a try/catch here because if the method throws an exception we want to 
            // close the connection throw code, because no datareader will exist, hence the 
            // commandBehaviour.CloseConnection will not work
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }

        /// <summary>
        /// Execute a SqlCommand that returns the first column of the first record against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a OleDbConnection</param>
        /// <param name="cmd">T-SQL command</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(string connectionString, SqlCommand cmd, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
        }

        /// <summary>
        /// Execute a SqlCommand that returns the first column of the first record against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a OleDbConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
        }

    }
}
