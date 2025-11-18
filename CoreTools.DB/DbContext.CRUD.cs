using System.Data;
using System.Linq.Expressions;
using SixLabors.ImageSharp;
using SqlSugar;
using DbType = SqlSugar.DbType;

namespace CoreTools.DB;

public partial class DbContext(string connectionString, DbType dbType = DbType.MySql)
{
    private readonly string _connectionString = connectionString;

    public SqlSugarClient GetDb()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new Exception("AAMSContext未初始化");
        }

        var connectionConfig = new ConnectionConfig()
        {
            ConnectionString = _connectionString,
            DbType = dbType,
            IsAutoCloseConnection = true,
        };

        if (CustomConfigureExternalService != null)
        {
            connectionConfig.ConfigureExternalServices = CustomConfigureExternalService;
        }

        SqlSugarClient db = new(connectionConfig);

        // 配置加密解密
        if (DataExecuting != null)
        {
            db.Aop.DataExecuting = DataExecuting;
        }

        if (DataExecuted != null)
        {
            db.Aop.DataExecuted = DataExecuted;
        }

        if (TraceLogEnabled)
        {
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                var sqlStr = $@"
===========================================================
|> TIME: {DateTime.Now:yyyy/MM/dd HH:mm:ss}
|> SQL: {sql}
|> Params: {string.Join(", ", pars?.Select(p => $"{p.ParameterName}={p.Value}") ?? ["null"])}
===========================================================
";
                if (TraceLogHandler != null)
                {
                    TraceLogHandler.Invoke(sqlStr);
                }
                else
                {
                    Console.WriteLine($"CoreTools.DB: {sqlStr}");
                }
            };

        }

        return db;
    }

    #region Insert
    public bool Insert<T>(T item) where T : class, new()
    {
        using var db = GetDb();
        item = Clone(item);
        return db.Insertable(item).ExecuteCommand() > 0;
    }
    #endregion

    #region Delete
    public bool Delete<T>(Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        using var db = GetDb();

        var affectedRows = db.Deleteable<T>()
                             .Where(whereExpression)
                             .ExecuteCommand();

        return affectedRows > 0;
    }

    public bool Delete<T>(T entity) where T : class, new()
    {
        using var db = GetDb();
        var affectedRows = db.Deleteable(entity).ExecuteCommand();
        return affectedRows > 0;
    }
    #endregion

    #region Update
    public bool Update<T>(T item) where T : class, new()
    {
        using var db = GetDb();
        item = Clone(item);
        return db.Updateable(item).ExecuteCommand() > 0;
    }

    public bool Update<T>(Expression<Func<T, T>> updateExpression,
        Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        using var db = GetDb();

        var affectedRows = db.Updateable<T>()
                             .SetColumns(updateExpression)
                             .Where(whereExpression)
                             .ExecuteCommand();

        return affectedRows > 0;
    }
    #endregion

    #region Get
    public T? Get<T>(Expression<Func<T, bool>>? whereExpression = null) where T : class, new()
    {
        using var db = GetDb();
        var query = db.Queryable<T>();

        if (whereExpression != null)
        {
            query = query.Where(whereExpression);
        }

        var list = query.Take(1).ToList(); // 取第一条
        return list.Count > 0 ? list[0] : null;
    }

    public List<T> GetAll<T>(
        Expression<Func<T, bool>>? whereExpression = null,
        Expression<Func<T, object>>? orderByExpression = null,
        OrderByType type = OrderByType.Asc,
        int top = 0,
        string? tableName = null) where T : class, new()
    {
        using var db = GetDb();

        var query = string.IsNullOrWhiteSpace(tableName) ? db.Queryable<T>() : db.Queryable<T>().AS(tableName);

        if (whereExpression != null)
        {
            query = query.Where(whereExpression);
        }

        if (orderByExpression != null)
        {
            query = query.OrderBy(orderByExpression, type);
        }

        // 约定：当 top>0 且 top < int.MaxValue 时才生效；否则视为不限制（支持你传 0 或 int.MaxValue 表示“无限”）
        if (top is > 0 and < int.MaxValue)
        {
            query = query.Take(top);
        }

        return query.ToList();
    }

    public List<T> GetAll<T>(
        Expression<Func<T, bool>> whereExpression,
        Expression<Func<T, object>> orderByExpression,
        out int total,
        OrderByType type = OrderByType.Asc,
        int skip = 0,
        int take = 0,
        string? tableName = null) where T : class, new()
    {
        using var db = GetDb();

        var query = string.IsNullOrWhiteSpace(tableName) ? db.Queryable<T>() : db.Queryable<T>().AS(tableName);

        if (whereExpression != null)
        {
            query = query.Where(whereExpression);
        }

        if (orderByExpression != null)
        {
            query = query.OrderBy(orderByExpression, type);
        }

        total = query.Count();

        if (skip > 0)
        {
            query = query.Skip(skip);
        }

        if (take > 0)
        {
            query = query.Take(take);
        }

        return query.ToList();
    }

    public List<T> GetAll<T>(
        Expression<Func<T, bool>> whereExpression,
        int skip,
        int take,
        out int total,
        string? tableName = null) where T : class, new()
    {
        using var db = GetDb();

        var query = string.IsNullOrWhiteSpace(tableName)
            ? db.Queryable<T>()
            : db.Queryable<T>().AS(tableName);

        if (whereExpression != null)
        {
            query = query.Where(whereExpression);
        }

        total = query.Count();

        if (skip > 0)
        {
            query = query.Skip(skip);
        }

        if (take > 0)
        {
            query = query.Take(take);
        }

        return query.ToList();
    }

    public bool Exists<T>(Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        using var db = GetDb();
        return db.Queryable<T>().Any(whereExpression);
    }

    public int Count(string sql, params SugarParameter[] parameters)
    {
        using var db = GetDb();

        // SqlSugar 提供 Ado 对象执行原生 SQL
        var result = db.Ado.GetDataTable(sql, parameters);

        return result.Rows.Count > 0 ? Convert.ToInt32(result.Rows[0][0]) : 0;
    }

    public int Count<T>(Expression<Func<T, bool>>? whereExpression = null) where T : class, new()
    {
        using var db = GetDb();

        var query = db.Queryable<T>();

        if (whereExpression != null)
        {
            query = query.Where(whereExpression);
        }

        return query.Count();
    }
    #endregion

    #region 执行原生SQL

    /// <summary>
    /// 执行原生 SQL 并返回 T 类型列表
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数化查询</param>
    /// <returns>实体列表</returns>
    public List<T> Execute<T>(string sql, params SugarParameter[] parameters) where T : class, new()
    {
        using var db = GetDb();
        var result = db.Ado.SqlQuery<T>(sql, parameters);

        return result;
    }

    public int ExecuteNonQuery(string sql, params SugarParameter[] parameters)
    {
        using var db = GetDb();
        return db.Ado.ExecuteCommand(sql, parameters);
    }

    public List<T> ExecuteQuery<T>(
        string sql,
        params SugarParameter[] parameters) where T : class, new()
    {
        using var db = GetDb();
        return db.Ado.SqlQuery<T>(sql, parameters);
    }
    #endregion
}

