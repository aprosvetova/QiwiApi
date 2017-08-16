using System;
using Newtonsoft.Json;

namespace QiwiApi.Models {
	public class History {
		[JsonProperty("data")]
		public Payment[] Payments;

		[JsonProperty("nextTxnId")]
		public long? NextTransactionId;

		[JsonProperty("nextTxnDate")]
		public DateTime? NextTransactionDate;
	}
}
