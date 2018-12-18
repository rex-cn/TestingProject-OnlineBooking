using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using OnlineBookingSupport.DBUtility;

namespace OnlineBookingSupport.DBUtility
{
    /// <summary>
    /// 数据访问抽象基础类
    /// Copyright (C) 2004-2008 By Leo He 
    /// </summary>

    public abstract class SqlHelper
    {
        private static string _connectionString = PubConstant.ConnectionString;

        //protected SqlHelper()
        //{
        //    _connectionString =PubConstant.ConnectionString;
        //}

        #region 公用方法

        /// <summary>
        /// 判断是否存在某表的某个字段
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <param name="columnName">列名称</param>
        /// <returns>是否存在</returns>
        public static bool ColumnExists(string tableName, string columnName)
        {
            string sql = "select count(1) from syscolumns where [id]=object_id('" + tableName + "') and [name]='" +
                         columnName + "'";
            object res = GetSingle(sql);
            if (res == null)
            {
                return false;
            }
            return Convert.ToInt32(res) > 0;
        }

        public static int GetMaxID(string fieldName, string tableName)
        {
            string strsql = "select max(" + fieldName + ")+1 from " + tableName;
            object obj = GetSingle(strsql);
            return obj == null ? 1 : int.Parse(obj.ToString());
        }

        public static bool Exists(string strSql)
        {
            object obj = GetSingle(strSql);
            int cmdresult = (Equals(obj, null)) || (Equals(obj, DBNull.Value)) ? 0 : int.Parse(obj.ToString());
            return cmdresult != 0;
        }

        /// <summary>
        /// 表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static bool TabExists(string tableName)
        {
            string strsql = "select count(*) from sysobjects where id = object_id(N'[" + tableName +
                            "]') and OBJECTPROPERTY(id, N'IsUserTable') = 1";
            object obj = GetSingle(strsql);
            return !Equals(obj, null) && !Equals(obj, DBNull.Value);
        }

        public static bool Exists(string strSql, params SqlParameter[] cmdParms)
        {
            object obj = GetSingle(strSql, cmdParms);
            return (obj == null || obj.ToString() != "0") && (!Equals(obj, null) && !Equals(obj, DBNull.Value));
        }

        #endregion

        #region  执行简单SQL语句

        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="SQLString">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string SQLString)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        return cmd.ExecuteNonQuery();
                    }
                    //catch (SqlException e)
                    //{
                    //    connection.Close();
                    //    throw e;
                    //}
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        public static int ExecuteSqlByTime(string SQLString, int Times)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandTimeout = Times;
                        return cmd.ExecuteNonQuery();
                    }
                    //catch (SqlException e)
                    //{

                        //    throw e;
                    //}
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行Sql和Oracle滴混合事务
        /// </summary>
        /// <param name="list">SQL命令行列表</param>
        /// <param name="oracleCmdSqlList">Oracle命令行列表</param>
        /// <returns>执行结果 0-由于SQL造成事务失败 -1 由于Oracle造成事务失败 1-整体事务执行成功</returns>
        public static int ExecuteSqlTran(List<CommandInfo> list, List<CommandInfo> oracleCmdSqlList)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand { Connection = conn };
                SqlTransaction tx = conn.BeginTransaction();
                cmd.Transaction = tx;
                try
                {
                    foreach (CommandInfo myDE in list)
                    {
                        string cmdText = myDE.CommandText;
                        var cmdParms = (SqlParameter[])myDE.Parameters;
                        PrepareCommand(cmd, conn, tx, cmdText, cmdParms);
                        if (myDE.EffentNextType == EffentNextType.SolicitationEvent)
                        {
                            if (myDE.CommandText.ToLower().IndexOf("count(") == -1)
                            {
                                tx.Rollback();
                                throw new Exception("违背要求" + myDE.CommandText + "必须符合select count(..的格式");
                                //return 0;
                            }

                            object obj = cmd.ExecuteScalar();
                            bool isHave = (obj != null || obj != DBNull.Value) && Convert.ToInt32(obj) > 0;
                            if (isHave)
                            {
                                //引发事件
                                myDE.OnSolicitationEvent();
                            }
                        }
                        if (myDE.EffentNextType == EffentNextType.WhenHaveContine ||
                            myDE.EffentNextType == EffentNextType.WhenNoHaveContine)
                        {
                            if (myDE.CommandText.ToLower().IndexOf("count(") == -1)
                            {
                                tx.Rollback();
                                throw new Exception("SQL:违背要求" + myDE.CommandText + "必须符合select count(..的格式");
                                //return 0;
                            }

                            object obj = cmd.ExecuteScalar();
                            bool isHave = (obj != null || obj != DBNull.Value) && Convert.ToInt32(obj) > 0;

                            if (myDE.EffentNextType == EffentNextType.WhenHaveContine && !isHave)
                            {
                                tx.Rollback();
                                throw new Exception("SQL:违背要求" + myDE.CommandText + "返回值必须大于0");
                                //return 0;
                            }
                            if (myDE.EffentNextType == EffentNextType.WhenNoHaveContine && isHave)
                            {
                                tx.Rollback();
                                throw new Exception("SQL:违背要求" + myDE.CommandText + "返回值必须等于0");
                                //return 0;
                            }
                            continue;
                        }
                        int val = cmd.ExecuteNonQuery();
                        if (myDE.EffentNextType == EffentNextType.ExcuteEffectRows && val == 0)
                        {
                            tx.Rollback();
                            throw new Exception("SQL:违背要求" + myDE.CommandText + "必须有影响行");
                            //return 0;
                        }
                        cmd.Parameters.Clear();
                    }
                    //string oraConnectionString = PubConstant.GetConnectionString("ConnectionStringPPC");
                    //bool res = OracleHelper.ExecuteSqlTran(oraConnectionString, oracleCmdSqlList);
                    //if (!res)
                    //{
                    //    tx.Rollback();
                    //    throw new Exception("Oracle执行失败");
                    //    // return -1;
                    //}
                    tx.Commit();
                    return 1;
                }
                catch (Exception e)
                {
                    tx.Rollback();
                    throw e;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="SQLStringList">多条SQL语句</param>		
        public static int ExecuteSqlTran(List<String> SQLStringList)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand();
                cmd.Connection = conn;
                SqlTransaction tx = conn.BeginTransaction();
                cmd.Transaction = tx;
                try
                {
                    int count = 0;
                    for (int n = 0; n < SQLStringList.Count; n++)
                    {
                        string strsql = SQLStringList[n];
                        if (strsql.Trim().Length > 1)
                        {
                            cmd.CommandText = strsql;
                            count += cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                    return count;
                }
                catch
                {
                    tx.Rollback();
                    return 0;
                }
            }
        }


        /// <summary>
        /// 执行带一个存储过程参数的的SQL语句。
        /// </summary>
        /// <param name="sqlString">SQL语句</param>
        /// <param name="content">参数内容,比如一个字段是格式复杂的文章，有特殊符号，可以通过这个方式添加</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string sqlString, string content)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(sqlString, connection);
                var myParameter = new SqlParameter("@content", SqlDbType.NText);
                myParameter.Value = content;
                cmd.Parameters.Add(myParameter);
                try
                {
                    connection.Open();
                    int rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 执行带一个存储过程参数的的SQL语句。
        /// </summary>
        /// <param name="SQLString">SQL语句</param>
        /// <param name="content">参数内容,比如一个字段是格式复杂的文章，有特殊符号，可以通过这个方式添加</param>
        /// <returns>影响的记录数</returns>
        public static object ExecuteSqlGet(string SQLString, string content)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(SQLString, connection);
                var myParameter = new SqlParameter("@content", SqlDbType.NText);
                myParameter.Value = content;
                cmd.Parameters.Add(myParameter);
                try
                {
                    connection.Open();
                    object obj = cmd.ExecuteScalar();
                    if ((Equals(obj, null)) || (Equals(obj, DBNull.Value)))
                    {
                        return null;
                    }
                    else
                    {
                        return obj;
                    }
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 向数据库里插入图像格式的字段(和上面情况类似的另一种实例)
        /// </summary>
        /// <param name="strSQL">SQL语句</param>
        /// <param name="fs">图像字节,数据库的字段类型为image的情况</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSqlInsertImg(string strSQL, byte[] fs)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(strSQL, connection);
                var myParameter = new SqlParameter("@fs", SqlDbType.Image);
                myParameter.Value = fs;
                cmd.Parameters.Add(myParameter);
                try
                {
                    connection.Open();
                    int rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sqlString">计算查询结果语句</param>
        /// <returns>查询结果（object）</returns>
        public static object GetSingle(string sqlString)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(sqlString, connection))
                {
                    try
                    {
                        connection.Open();
                        object obj = cmd.ExecuteScalar();
                        return (Equals(obj, null)) || (Equals(obj, DBNull.Value)) ? null : obj;
                    }
                    catch (SqlException e)
                    {
                        connection.Close();
                        throw e;
                    }
                }
            }
        }

        public static object GetSingle(string sqlString, int times)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(sqlString, connection))
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandTimeout = times;
                        object obj = cmd.ExecuteScalar();
                        return (Equals(obj, null)) || (Equals(obj, DBNull.Value)) ? null : obj;
                    }
                    catch (SqlException e)
                    {
                        connection.Close();
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="SQLString">查询语句</param>
        /// <returns>DataSet</returns>
        public static DataSet Query(string SQLString)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var ds = new DataSet();
                try
                {
                    connection.Open();
                    var command = new SqlDataAdapter(SQLString, connection);
                    command.Fill(ds, "ds");
                }
                catch (SqlException ex)
                {
                    throw new Exception(ex.Message);
                }
                return ds;
            }
        }

        public static DataSet Query(string SQLString, int Times)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var ds = new DataSet();
                try
                {
                    connection.Open();
                    var command = new SqlDataAdapter(SQLString, connection);
                    command.SelectCommand.CommandTimeout = Times;
                    command.Fill(ds, "ds");
                }
                catch (SqlException ex)
                {
                    throw new Exception(ex.Message);
                }
                return ds;
            }
        }

        public static DataTable ExecuteDataTable(string cmdText, SqlParameter[] parms, CommandType cmdtype)
        {
            var dt = new DataTable();

            var conn = new SqlConnection(_connectionString);
            var apt = new SqlDataAdapter(cmdText, conn);
            apt.SelectCommand.CommandType = cmdtype;

            if (parms != null)
            {
                //添加参数
                foreach (SqlParameter parm in parms)
                {
                    var p = (DbParameter)((ICloneable)parm).Clone();
                    apt.SelectCommand.Parameters.Add(p);
                }
            }
            apt.Fill(dt);
            apt.SelectCommand.Parameters.Clear();
            return dt;
        }

        public static DataTable ExecuteDataTable(string cmdText, CommandType cmdtype)
        {
            var dt = new DataTable();

            var conn = new SqlConnection(_connectionString);
            var apt = new SqlDataAdapter(cmdText, conn);
            apt.SelectCommand.CommandType = cmdtype;
            apt.Fill(dt);

            return dt;
        }

        public static DataTable ExecuteDataTable(string cmdText)
        {
            var dt = new DataTable();

            var conn = new SqlConnection(_connectionString);
            var apt = new SqlDataAdapter(cmdText, conn);
            apt.SelectCommand.CommandType = CommandType.Text;
            apt.Fill(dt);
            return dt;
        }

        public static DataTable ExecuteDataTable(SqlTransaction tran, string cmdText)
        {
            var dt = new DataTable();

            SqlConnection conn = tran.Connection;
            var apt = new SqlDataAdapter(cmdText, conn);
            apt.SelectCommand.CommandType = CommandType.Text;
            apt.SelectCommand.Transaction = tran;
            apt.Fill(dt);
            return dt;
        }

        #region 执行多条带参数的SQL语句，实现数据库事务。

        public static bool ExecuteSqlParamTran(List<String> sqlStringList, List<SqlParameter[]> prams)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                bool success = true;
                conn.Open();
                var comm = new SqlCommand();
                comm.Connection = conn;
                SqlTransaction tx = conn.BeginTransaction();
                comm.Transaction = tx;

                try
                {
                    for (int i = 0; i < sqlStringList.Count; i++)
                    {
                        //SqlCommand comm = new SqlCommand(sqlStringList[i],conn);
                        //comm.Transaction = tx;
                        comm.CommandText = sqlStringList[i];
                        if (prams.Count > 0)
                        {
                            foreach (SqlParameter p in prams[i])
                            {
                                comm.Parameters.Add(p);
                            }
                        }
                        else
                        {
                            return false;
                        }
                        comm.ExecuteNonQuery();
                        comm.Parameters.Clear();
                    }
                    tx.Commit();
                }
                catch
                {
                    success = false;
                    tx.Rollback();
                }
                return success;
            }
        }

        #endregion

        #endregion

        #region 执行带参数的SQL语句

        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="SQLString">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string SQLString, params SqlParameter[] cmdParms)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, connection, null, SQLString, cmdParms);
                        int rows = cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                        return rows;
                    }
                    catch (SqlException e)
                    {
                        throw e;
                    }
                }
            }
        }


        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="sqlStringList">SQL语句的哈希表（key为sql语句，value是该语句的SqlParameter[]）</param>
        public static void ExecuteSqlTran(Hashtable sqlStringList)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    var cmd = new SqlCommand();
                    try
                    {
                        //循环
                        foreach (DictionaryEntry myDe in sqlStringList)
                        {
                            string cmdText = myDe.Key.ToString();
                            var cmdParms = (SqlParameter[])myDe.Value;
                            PrepareCommand(cmd, conn, trans, cmdText, cmdParms);
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                        }
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="cmdList">SQL语句的哈希表（key为sql语句，value是该语句的SqlParameter[]）</param>
        public static int ExecuteSqlTran(List<CommandInfo> cmdList)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    var cmd = new SqlCommand();
                    try
                    {
                        int count = 0;
                        //循环
                        foreach (CommandInfo myDE in cmdList)
                        {
                            string cmdText = myDE.CommandText;
                            var cmdParms = (SqlParameter[])myDE.Parameters;
                            PrepareCommand(cmd, conn, trans, cmdText, cmdParms);

                            if (myDE.EffentNextType == EffentNextType.WhenHaveContine ||
                                myDE.EffentNextType == EffentNextType.WhenNoHaveContine)
                            {
                                if (myDE.CommandText.ToLower().IndexOf("count(") == -1)
                                {
                                    trans.Rollback();
                                    return 0;
                                }

                                object obj = cmd.ExecuteScalar();
                                bool isHave;
                                if (obj == null && obj == DBNull.Value)
                                {
                                    isHave = false;
                                }
                                isHave = Convert.ToInt32(obj) > 0;

                                if (myDE.EffentNextType == EffentNextType.WhenHaveContine && !isHave)
                                {
                                    trans.Rollback();
                                    return 0;
                                }
                                if (myDE.EffentNextType == EffentNextType.WhenNoHaveContine && isHave)
                                {
                                    trans.Rollback();
                                    return 0;
                                }
                                continue;
                            }
                            int val = cmd.ExecuteNonQuery();
                            count += val;
                            if (myDE.EffentNextType == EffentNextType.ExcuteEffectRows && val == 0)
                            {
                                trans.Rollback();
                                return 0;
                            }
                            cmd.Parameters.Clear();
                        }
                        trans.Commit();
                        return count;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="SQLStringList">SQL语句的哈希表（key为sql语句，value是该语句的SqlParameter[]）</param>
        public static void ExecuteSqlTranWithIndentity(List<CommandInfo> SQLStringList)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    var cmd = new SqlCommand();
                    try
                    {
                        int indentity = 0;
                        //循环
                        foreach (CommandInfo myDE in SQLStringList)
                        {
                            string cmdText = myDE.CommandText;
                            var cmdParms = (SqlParameter[])myDE.Parameters;
                            foreach (SqlParameter q in cmdParms)
                            {
                                if (q.Direction == ParameterDirection.InputOutput)
                                {
                                    q.Value = indentity;
                                }
                            }
                            PrepareCommand(cmd, conn, trans, cmdText, cmdParms);
                            int val = cmd.ExecuteNonQuery();
                            foreach (SqlParameter q in cmdParms)
                            {
                                if (q.Direction == ParameterDirection.Output)
                                {
                                    indentity = Convert.ToInt32(q.Value);
                                }
                            }
                            cmd.Parameters.Clear();
                        }
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="sqlStringList">SQL语句的哈希表（key为sql语句，value是该语句的SqlParameter[]）</param>
        public static void ExecuteSqlTranWithIndentity(Hashtable sqlStringList)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    var cmd = new SqlCommand();
                    try
                    {
                        int indentity = 0;
                        //循环
                        foreach (DictionaryEntry myDe in sqlStringList)
                        {
                            string cmdText = myDe.Key.ToString();
                            var cmdParms = (SqlParameter[])myDe.Value;
                            foreach (SqlParameter q in cmdParms)
                            {
                                if (q.Direction == ParameterDirection.InputOutput)
                                {
                                    q.Value = indentity;
                                }
                            }
                            PrepareCommand(cmd, conn, trans, cmdText, cmdParms);
                            cmd.ExecuteNonQuery();
                            foreach (SqlParameter q in cmdParms)
                            {
                                if (q.Direction == ParameterDirection.Output)
                                {
                                    indentity = Convert.ToInt32(q.Value);
                                }
                            }
                            cmd.Parameters.Clear();
                        }
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sqlString">计算查询结果语句</param>
        /// <param name="cmdParms"></param>
        /// <returns>查询结果（object）</returns>
        public static object GetSingle(string sqlString, params SqlParameter[] cmdParms)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand())
                {
                    PrepareCommand(cmd, connection, null, sqlString, cmdParms);
                    object obj = cmd.ExecuteScalar();
                    cmd.Parameters.Clear();
                    return (Equals(obj, null)) || (Equals(obj, DBNull.Value)) ? null : obj;
                }
            }
        }

        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="sqlString">查询语句</param>
        /// <param name="cmdParms"></param>
        /// <returns>DataSet</returns>
        public static DataSet Query(string sqlString, params SqlParameter[] cmdParms)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand();
                PrepareCommand(cmd, connection, null, sqlString, cmdParms);
                using (var da = new SqlDataAdapter(cmd))
                {
                    var ds = new DataSet();
                    try
                    {
                        da.Fill(ds, "ds");
                        cmd.Parameters.Clear();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    return ds;
                }
            }
        }


        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, string cmdText,
                                           SqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = CommandType.Text; //cmdType;
            if (cmdParms != null)
            {
                foreach (SqlParameter parameter in cmdParms)
                {
                    if ((parameter.Direction == ParameterDirection.InputOutput ||
                         parameter.Direction == ParameterDirection.Input) &&
                        (parameter.Value == null))
                    {
                        parameter.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(parameter);
                }
            }
        }

        #endregion

        #region 存储过程操作

        /// <summary>
        /// 执行存储过程，返回Int
        /// </summary>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <returns>存储过程RETURN命令返回值</returns>
        public static int RunProcedureReturnInt(string storedProcName, IDataParameter[] parameters)
        {
            int returnInt;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                //SqlCommand command = BuildQueryCommand(connection, storedProcName, parameters);
                using (var command = new SqlCommand(storedProcName))
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    foreach (SqlParameter sp in parameters)
                    {
                        command.Parameters.Add(sp);
                    }
                    command.Parameters.Add(new SqlParameter("@Return", SqlDbType.Int, 4));
                    command.Parameters[command.Parameters.Count - 1].Direction = ParameterDirection.ReturnValue;
                    command.ExecuteScalar();
                    returnInt = (int)command.Parameters["@Return"].Value;
                }
                connection.Close();
            }
            return returnInt;
        }


        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <param name="tableName">DataSet结果中的表名</param>
        /// <returns>DataSet</returns>
        public static DataSet RunProcedure(string storedProcName, IDataParameter[] parameters, string tableName)
        {
            //ConnectionString = PubConstant.ConnectionString;
            using (var connection = new SqlConnection(_connectionString))
            {
                var dataSet = new DataSet();
                connection.Open();
                var sqlDa = new SqlDataAdapter();
                sqlDa.SelectCommand = BuildQueryCommand(connection, storedProcName, parameters);
                sqlDa.Fill(dataSet, tableName);
                connection.Close();
                return dataSet;
            }
        }

        public static DataSet RunProcedure(string storedProcName, IDataParameter[] parameters, string tableName,
                                           int times)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var dataSet = new DataSet();
                connection.Open();
                var sqlDa = new SqlDataAdapter
                {
                    SelectCommand = BuildQueryCommand(connection, storedProcName, parameters)
                };
                sqlDa.SelectCommand.CommandTimeout = times;
                sqlDa.Fill(dataSet, tableName);
                connection.Close();
                return dataSet;
            }
        }


        /// <summary>
        /// 构建 SqlCommand 对象(用来返回一个结果集，而不是一个整数值)
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <returns>SqlCommand</returns>
        private static SqlCommand BuildQueryCommand(SqlConnection connection, string storedProcName,
                                                    IEnumerable<IDataParameter> parameters)
        {
            var command = new SqlCommand(storedProcName, connection) { CommandType = CommandType.StoredProcedure };
            foreach (SqlParameter parameter in parameters)
            {
                if (parameter != null)
                {
                    // 检查未分配值的输出参数,将其分配以DBNull.Value.
                    if ((parameter.Direction == ParameterDirection.InputOutput ||
                         parameter.Direction == ParameterDirection.Input) &&
                        (parameter.Value == null))
                    {
                        parameter.Value = DBNull.Value;
                    }
                    var p = (DbParameter)((ICloneable)parameter).Clone();
                    command.Parameters.Add(p);
                }
            }

            return command;
        }

        /// <summary>
        /// 执行存储过程，返回影响的行数		
        /// </summary>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <param name="rowsAffected">影响的行数</param>
        /// <returns></returns>
        public static int RunProcedure(string storedProcName, IDataParameter[] parameters, out int rowsAffected)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = BuildIntCommand(connection, storedProcName, parameters);
                rowsAffected = command.ExecuteNonQuery();
                var result = (int)command.Parameters["ReturnValue"].Value;
                connection.Close();
                return result;
            }
        }

        /// <summary>
        /// 创建 SqlCommand 对象实例(用来返回一个整数值)	
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="storedProcName">存储过程名</param>
        /// <param name="parameters">存储过程参数</param>
        /// <returns>SqlCommand 对象实例</returns>
        private static SqlCommand BuildIntCommand(SqlConnection connection, string storedProcName,
                                                  IEnumerable<IDataParameter> parameters)
        {
            SqlCommand command = BuildQueryCommand(connection, storedProcName, parameters);
            command.Parameters.Add(new SqlParameter("ReturnValue",
                                                    SqlDbType.Int, 4, ParameterDirection.ReturnValue,
                                                    false, 0, 0, string.Empty, DataRowVersion.Default, null));
            return command;
        }

        #endregion
        public static DataSet GetList(string tbName, int PageSize, int PageIndex, string strWhere, string orderFldName, int descs)
        {
            SqlParameter[] parameters = {
                                            new SqlParameter("@tblName", tbName),
                                            new SqlParameter("@fldName", "*"),
                                            new SqlParameter("@OrderfldName",orderFldName), //-- 排序字段名 
                                            new SqlParameter("@PageSize", PageSize),
                                            new SqlParameter("@PageIndex", PageIndex), //页码
                                            new SqlParameter("@IsReCount", 1), //返回记录总数, 非 0 值则返回
                                            new SqlParameter("@OrderType", descs), //设置排序类型, 非 0 值则降序 
                                            new SqlParameter("@strWhere", strWhere),
                                        };

            return SqlHelper.RunProcedure("UP_GetRecordByPageOrder", parameters, "ds");
        }
    }
}