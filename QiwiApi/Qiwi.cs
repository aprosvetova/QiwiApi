using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using QiwiApi.Events;
using QiwiApi.Models;
using QiwiApi.Models.Enums;
using QiwiApi.Requests;

namespace QiwiApi {
	public class Qiwi {
		private const string BASE = "https://edge.qiwi.com/";
		private string _phone;
		private string _token;
		private readonly WebClient _webClient;
		private long? _nextTxId = null;
		private DateTime? _nextTxDate = null;
		private List<long> _handledTransactions = new List<long>();
		private long? _lastHandledTransaction;
		private CancellationTokenSource _receivingCancellationTokenSource;

		public Qiwi(string phone, string token, long? nextTxId = null, DateTime? nextTxDate = null) {
			_phone = phone;
			_token = token;
			_nextTxId = nextTxId;
			_nextTxDate = nextTxDate;
			_webClient = new WebClient {
				Encoding = Encoding.UTF8
			};
		}

		private void SetNextTransaction(long? nextTxId, DateTime? nextTxDate) {
			if (_nextTxId != nextTxId || _nextTxDate != nextTxDate) {
				OnNextTransactionUpdate?.Invoke(this, new NextTransactionUpdateEventArgs(nextTxId, nextTxDate));
			}
			_nextTxId = nextTxId;
			_nextTxDate = nextTxDate;
		}

		public void SetPhone(string phone) {
			_phone = phone;
		}

		public void SetToken(string token) {
			_token = token;
		}

		public void StartHistoryPolling(TimeSpan? period = null, CancellationToken cancellationToken = default(CancellationToken)) {
			_receivingCancellationTokenSource = new CancellationTokenSource();
			cancellationToken.Register(() => _receivingCancellationTokenSource.Cancel());
			if (period == null) {
				period = TimeSpan.FromMinutes(1);
			}
			PollHistoryAsync(period.Value, _receivingCancellationTokenSource.Token);
		}

		private async void PollHistoryAsync(TimeSpan period, CancellationToken cancellationToken = default(CancellationToken)) {
			while (!cancellationToken.IsCancellationRequested) {
				try {
					var history = await GetHistoryAsync(_nextTxId, _nextTxDate, cancellationToken).ConfigureAwait(false);
					if (history.Payments.Any()) {
						if (!history.Payments.Any(x => x.Id == _lastHandledTransaction)) {
							_handledTransactions.Clear();
						}
						_lastHandledTransaction = history.Payments[0].Id;
					}
					SetNextTransaction(history.NextTransactionId, history.NextTransactionDate);
					foreach (var payment in history.Payments) {
						if (!_handledTransactions.Contains(payment.Id)) {
							OnNewPayment(payment);
							_handledTransactions.Add(payment.Id);
						}
					}
					Thread.Sleep(period);
				} catch (Exception generalException) {
					// handle later
				}
			}
		}

		public async Task<Balances> GetBalancesAsync() {
			_webClient.Headers["Authorization"] = $"Bearer {_token}";
			var url = BuildUrl("funding-sources/v1/accounts/current");
			var response = await _webClient.DownloadStringTaskAsync(url);
			var deserialized = JsonConvert.DeserializeObject<dynamic>(response);
			var balances = new Balances();
			foreach (var account in deserialized.accounts) {
				switch (account.alias.ToString()) {
					case "qw_wallet_rub":
						balances.Rub = account.balance.amount;
						break;
					case "qw_wallet_usd":
						balances.Usd = account.balance.amount;
						break;
					case "qw_wallet_eur":
						balances.Eur = account.balance.amount;
						break;
					case "qw_wallet_kzt":
						balances.Kzt = account.balance.amount;
						break;
				}
			}
			return balances;
		}

		public async Task<bool> SendMoneyToWallet(string phone, decimal amount, string comment = null) {
			var request = new MoneyTransfer {
				Id = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString(),
				Sum = new Sum {
					Amount = amount.ToString(CultureInfo.InvariantCulture),
					Currency = "643"
				},
				Source = "account_643",
				PaymentMethod = new PaymentMethod {
					Type = "Account",
					AccountId = "643"
				},
				Comment = comment,
				Fields = new Fields {
					Account = "+" + phone
				}
			};
			_webClient.Headers["Authorization"] = $"Bearer {_token}";
			_webClient.Headers["Content-Type"] = "application/json";
			var url = BuildUrl("sinap/terms/99/payments");
			try {
				var response = await _webClient.UploadStringTaskAsync(url, JsonConvert.SerializeObject(request, new JsonSerializerSettings {
					ContractResolver = new CamelCasePropertyNamesContractResolver()
				}));
				return response.Contains("Accepted");
			} catch {
				return false;
			}
		}

		public async Task<History> GetHistoryAsync(long? nextTxId = null, DateTime? nextTxDate = null, CancellationToken cancellationToken = default(CancellationToken)) {
			_webClient.Headers["Authorization"] = $"Bearer {_token}";
			var parameters = new Dictionary<string, string>();
			if (nextTxId != null && nextTxDate != null) {
				parameters["nextTxnId"] = nextTxId.ToString();
				parameters["nextTxDate"] = nextTxDate.Value.ToString("yyyy-MM-ddTHH:mm:sszzz");
			}
			parameters["rows"] = "50";
			var url = BuildUrl($"payment-history/v1/persons/{_phone}/payments", parameters);
			var response = await _webClient.DownloadStringTaskAsync(url);
			var history = JsonConvert.DeserializeObject<History>(response);
			return history;
		}

		private static Uri BuildUrl(string additionalUrl, Dictionary<string, string> parameters = null) {
			var urlBuilder = new UriBuilder(BASE+additionalUrl);
			if (parameters != null) {
				urlBuilder.Query = string.Join("&", parameters.Select(kvp =>
					string.Format("{0}={1}", kvp.Key, kvp.Value)));
			}
			return urlBuilder.Uri;
		}

		public void StopHistoryPolling() {
			_receivingCancellationTokenSource.Cancel();
		}

		protected virtual void OnNewPayment(Payment payment) {
			var e = new PaymentEventArgs(payment);
			OnPayment?.Invoke(this, e);

			switch (payment.Type) {
				case PaymentType.In:
					OnIncomingPayment?.Invoke(this, e);
					break;
				case PaymentType.Out:
					OnOutgoingPayment?.Invoke(this, e);
					break;
			}
		}

		public event EventHandler<PaymentEventArgs> OnPayment;

		public event EventHandler<PaymentEventArgs> OnIncomingPayment;

		public event EventHandler<PaymentEventArgs> OnOutgoingPayment;

		public event EventHandler<NextTransactionUpdateEventArgs> OnNextTransactionUpdate;
	}
}