using Dapper;
using POS.Data.Interface;
using System.Data;
using POS.Models;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using POS.Data.Service;

namespace POS.Data.Reporsitory
{
	public class Repository : IInterface, IDisposable
	{
		private readonly string _connectionString;
		private readonly string _logPath;
		private IDbConnection? _connection;

		// DI Constructor
		public Repository(IConfiguration configuration)
		{
			_connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException(message: "Connection string 'DefaultConnection' not found.");
			_logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SystemErrorLogs");
			EnsureLogDirectoryExists();
		}

		// Direct Instantiation
		public Repository()
		{
			try
			{
				var builder = new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

				var configuration = builder.Build();
				_connectionString = configuration.GetConnectionString("DefaultConnection")
					?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

				_logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Logs");

				EnsureLogDirectoryExists();
			}
			catch (Exception ex)
			{
				throw new Exception($"Error loading configuration: {ex.Message}", ex);
			}
		}

		private void EnsureLogDirectoryExists()
		{
			try
			{
				if (!Directory.Exists(_logPath))
					Directory.CreateDirectory(_logPath);
			}
			catch
			{
				// Never crash the entire application because an error log folder couldn't be created
			}
		}


		private void LogError(Exception ex, string query, object? param, [CallerMemberName] string methodName = "")
		{
			try
			{
				string fileName = $"{AppDate.Now:yyyy-MM-dd}.txt";
				string filePath = Path.Combine(_logPath, fileName);

				string paramString = param != null ? Newtonsoft.Json.JsonConvert.SerializeObject(param) : "NULL";

				// 1. FILE SYSTEM FALLBACK LOGGING
				try
				{
					string logEntry = $@"[{AppDate.Now:HH:mm:ss}] ERROR in Method: {methodName}
--------------------------------------------------------------------------------
Query: {query}
Parameters: {paramString}
--------------------------------------------------------------------------------
Exception Type: {ex.GetType().Name}
Message: {ex.Message}
Stack Trace: {ex.StackTrace}
--------------------------------------------------------------------------------
Inner Exception: {ex.InnerException?.Message ?? "None"}
================================================================================

";
					File.AppendAllText(filePath, logEntry);
				}
				catch { }

				// 2. DATABASE LOGGING (Easily Readable on Plesk)
				try
				{
					using var connection = new SqlConnection(_connectionString);
					// Open connection explicitly if needed, but Dapper does it automatically
					string insertSql = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemErrorLogs')
                        BEGIN
                            CREATE TABLE SystemErrorLogs (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                LogDate DATETIME NOT NULL,
                                UserId INT NULL,
                                MethodName NVARCHAR(255),
                                Query NVARCHAR(MAX),
                                Parameters NVARCHAR(MAX),
                                ExceptionType NVARCHAR(255),
                                Message NVARCHAR(MAX),
                                StackTrace NVARCHAR(MAX),
                                InnerException NVARCHAR(MAX)
                            )
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemErrorLogs') AND name = 'UserId')
                            BEGIN
                                ALTER TABLE SystemErrorLogs ADD UserId INT NULL;
                            END
                        END;

                        INSERT INTO SystemErrorLogs 
                        (LogDate, UserId, MethodName, Query, Parameters, ExceptionType, Message, StackTrace, InnerException) 
                        VALUES (@LogDate, @UserId, @MethodName, @Query, @Parameters, @ExceptionType, @Message, @StackTrace, @InnerException)";

					int currentUserId = 0;
					try
					{
						currentUserId = UserHelper.GetCurrentUserId();
					}
					catch
					{
						// Ignore any errors while getting the user id
					}

					connection.Execute(insertSql, new
					{
						LogDate = AppDate.Now,
						UserId = currentUserId,
						MethodName = methodName ?? "",
						Query = query ?? "",
						Parameters = paramString,
						ExceptionType = ex.GetType().Name,
						Message = ex.Message ?? "",
						StackTrace = ex.StackTrace ?? "",
						InnerException = ex.InnerException?.Message ?? ""
					});
				}
				catch { }
			}
			catch
			{
				// Silent fail - can't log logging error
			}
		}

		public List<T> GetList<T>(string query, object? param = null)
		{
			try
			{
				using var connection = new SqlConnection(_connectionString);
				return connection.Query<T>(query, param, commandTimeout: 300).AsList();
			}
			catch (Exception ex)
			{
				LogError(ex, query, param);
				throw new Exception($"Error executing query: {ex.Message}", ex);
			}
		}

		public T? GetSingleValue<T>(string query, object? param = null)
		{
			try
			{
				using var connection = new SqlConnection(_connectionString);
				return connection.QueryFirstOrDefault<T>(query, param, commandTimeout: 300);
			}
			catch (Exception ex)
			{
				LogError(ex, query, param);
				throw new Exception($"Error executing query: {ex.Message}", ex);
			}
		}

		public T? GetSingleRow<T>(string query, object? param = null)
		{
			try
			{
				using var connection = new SqlConnection(_connectionString);
				return connection.QuerySingleOrDefault<T>(query, param, commandTimeout: 300);
			}
			catch (Exception ex)
			{
				LogError(ex, query, param);
				throw new Exception($"Error executing query: {ex.Message}", ex);
			}
		}

		public bool SaveData(string query, object? param = null)
		{
			try
			{
				using var connection = new SqlConnection(_connectionString);
				return connection.Execute(query, param, commandTimeout: 300) > 0;
			}
			catch (Exception ex)
			{
				LogError(ex, query, param);
				throw new Exception($"Error saving data: {ex.Message}", ex);
			}
		}

		public int SaveDataList(string query, List<object> paramList)
		{
			try
			{
				using var connection = new SqlConnection(_connectionString);
				return connection.Execute(query, paramList, commandTimeout: 300);
			}
			catch (Exception ex)
			{
				LogError(ex, query, paramList);
				throw new Exception($"Error saving data list: {ex.Message}", ex);
			}
		}

		public bool ExecuteTransaction(List<(string query, object? param)> operations)
		{
			using var connection = new SqlConnection(_connectionString);
			try
			{
				connection.Open();
				using var transaction = connection.BeginTransaction();
				try
				{
					foreach (var (query, param) in operations)
					{
						connection.Execute(query, param, transaction, commandTimeout: 300);
					}
					transaction.Commit();
					return true;
				}
				catch (Exception ex)
				{
					transaction.Rollback();

					// Log all operations that failed
					string allQueries = string.Join("\n", operations.Select((op, index) =>
						$"[{index + 1}] Query: {op.query} | Params: {Newtonsoft.Json.JsonConvert.SerializeObject(op.param)}"));

					LogError(ex, $"TRANSACTION FAILED:\n{allQueries}", null);

					throw new Exception($"Transaction failed and rolled back: {ex.Message}", ex);
				}
			}
			catch (Exception ex)
			{
				LogError(ex, "TRANSACTION CONNECTION ERROR", null);
				throw new Exception($"Database connection failed: {ex.Message}", ex);
			}
		}

		public void LogErrorToFile(Exception ex, string message = "")
		{
			LogError(ex, message, null);
		}

		public void Dispose()
		{
			try
			{
				_connection?.Close();
				_connection?.Dispose();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error disposing connection: {ex.Message}");
			}
		}
	}
}
