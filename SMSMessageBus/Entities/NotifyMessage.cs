using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSMessageBus.Entities
{
    [Serializable]
    public class NotifyMessage
    {
        /// <summary>
        /// RowKey
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Partition Key
        /// </summary>
        public string EntityID { get; set; }

        public string EmployeeID { get; set; }

        public string Header { get; set; }

        public string Message { get; set; }

        public DateTime CreatedOn { get; set; }

        public string CreatedById { get; set; }

    }
}
