using System;
namespace QiwiApi.Events {
	public class NextTransactionUpdateEventArgs {
		public long? NextTransactionId { get; private set; }

		public DateTime? NextTransactionDate { get; private set; }

		internal NextTransactionUpdateEventArgs(long? nextTransactionId, DateTime? nextTransactionDate) {
			NextTransactionId = nextTransactionId;
			NextTransactionDate = nextTransactionDate;
		}
	}
}
