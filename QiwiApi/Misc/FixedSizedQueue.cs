using System;
using System.Collections.Concurrent;

namespace QiwiApi.Misc {
	public class FixedSizedQueue<T> {
		public ConcurrentQueue<T> Queue = new ConcurrentQueue<T>();
		private object _lockObject = new object();
		private readonly int _limit;

		public FixedSizedQueue(int limit) {
			_limit = limit;
		}

		public void Enqueue(T obj) {
			Queue.Enqueue(obj);
			lock (_lockObject) {
				T overflow;
				while (Queue.Count > _limit && Queue.TryDequeue(out overflow)) ;
			}
		}
	}
}
