using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using System.Configuration;

namespace SMSMessageBus
{

    public class TracingHelper
    {
        static TableManager _tableManager = new TableManager("Notification");
        
        public static void LogMessage(Notification notificationMessage)
        {
            DateTime stamp = new DateTime();
            stamp = DateTime.UtcNow;
            notificationMessage.PartitionKey = notificationMessage.State.ToString();
            notificationMessage.RowKey = string.Format("{0}_{1}_{2}",notificationMessage.ID,notificationMessage.State,"UTC"+ stamp.ToString("yyyy-MMM-dd") + "-" +  stamp.ToString("hh:mm:ss:fffffff"));
            _tableManager.InsertEntity(notificationMessage);
        }
    }

    public class TableManager
    {
        private CloudTable table;

        public TableManager(string tableName)
        {
            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"].ToString();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
        }


        public  void InsertEntity<T>(T entity, bool forInsert = true) where T : TableEntity, new()
        {
            try
            {
                if (forInsert)
                {
                    var insertOperation = TableOperation.Insert(entity);
                    table.Execute(insertOperation);
                }
                else
                {
                    var insertOrMergeOperation = TableOperation.InsertOrReplace(entity);
                    table.Execute(insertOrMergeOperation);
                }
            }
            catch (Exception ExceptionObj)
            {
                throw ExceptionObj;
            }
        }

        public List<T> RetrieveEntity<T>(string Query = null) where T : TableEntity, new()
        {
            try
            {
                // Create the Table Query Object for Azure Table Storage  
                TableQuery<T> DataTableQuery = new TableQuery<T>();
                if (!String.IsNullOrEmpty(Query))
                {
                    DataTableQuery = new TableQuery<T>().Where(Query);
                }
                IEnumerable<T> IDataList = table.ExecuteQuery(DataTableQuery);
                List<T> DataList = new List<T>();
                foreach (var singleData in IDataList)
                    DataList.Add(singleData);
                return DataList;
            }
            catch (Exception ExceptionObj)
            {
                throw ExceptionObj;
            }
        }

        public bool DeleteEntity<T>(T entity) where T : TableEntity, new()
        {
            try
            {
                var DeleteOperation = TableOperation.Delete(entity);
                table.Execute(DeleteOperation);
                return true;
            }
            catch (Exception ExceptionObj)
            {
                throw ExceptionObj;
            }
        }
    }

}
