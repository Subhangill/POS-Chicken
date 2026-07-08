namespace POS.Data.Interface
{
	public interface IInterface
	{
		List<T> GetList<T>(string query, object? param = null);
		T? GetSingleValue<T>(string query, object? param = null);
		T? GetSingleRow<T>(string query, object? param = null);
		bool SaveData(string query, object? param = null);
		int SaveDataList(string query, List<object> paramList);
		bool ExecuteTransaction(List<(string query, object? param)> operations);
		void LogErrorToFile(Exception ex, string message = "");

	}
}
