using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Search;

namespace api.nox.search.client {
	public class WorkerTask {
		internal int Uid = Guid.NewGuid().GetHashCode();

		internal IWorker                 Worker;
		internal double                  Timeout;
		internal FetchOptions            Data;
		internal CancellationTokenSource CancellationToken;
		internal IResult                 Result;
		internal WorkerTaskStatus        Status;
		private  DateTime                _t0 = DateTime.MinValue;
		private  DateTime                _t1 = DateTime.MinValue;

		private double Elapsed
			=> Status != WorkerTaskStatus.Pending && Status != WorkerTaskStatus.Fetching
				? (float)(_t1          - _t0).TotalSeconds
				: (float)(DateTime.Now - _t0).TotalSeconds;


		internal string   MessageKey  = string.Empty;
		internal string[] MessageArgs = Array.Empty<string>();

		internal void Cancel()
			=> CancellationToken.Cancel();

		internal async UniTask Execute(SearchPage page) {
			if (CancellationToken.Token.IsCancellationRequested) {
				Status      = WorkerTaskStatus.Canceled;
				MessageKey  = "search.worker.canceled";
				MessageArgs = Array.Empty<string>();
				page.OnWorkerTaskUpdate.Invoke(this);
				return;
			}

			Status      = WorkerTaskStatus.Pending;
			MessageKey  = string.Empty;
			MessageArgs = Array.Empty<string>();
			Result      = null;
			page.OnWorkerTaskUpdate.Invoke(this);

			_t0 = DateTime.Now;

			var result = Worker.Fetch(Data).AttachExternalCancellation(CancellationToken.Token);

			Status      = WorkerTaskStatus.Fetching;
			MessageKey  = "search.worker.fetching";
			MessageArgs = Array.Empty<string>();
			page.OnWorkerTaskUpdate.Invoke(this);
			await UniTask.WaitUntil(() => result.Status != UniTaskStatus.Pending || Elapsed > Timeout);

			if (CancellationToken.Token.IsCancellationRequested) {
				Status     = WorkerTaskStatus.Canceled;
				MessageKey = "search.worker.canceled";
				page.OnWorkerTaskUpdate.Invoke(this);
				return;
			}

			if (result.Status != UniTaskStatus.Pending)
				CancellationToken.Cancel();

			switch (result.Status) {
				case UniTaskStatus.Canceled:
					Status      = WorkerTaskStatus.Canceled;
					MessageKey  = "search.worker.canceled";
					MessageArgs = Array.Empty<string>();
					page.OnWorkerTaskUpdate.Invoke(this);
					return;
				case UniTaskStatus.Faulted:
					try {
						await result;
					} catch (Exception ex) {
						Logger.LogError(ex);
						Status      = WorkerTaskStatus.Faulted;
						MessageKey  = "search.worker.error";
						MessageArgs = new[] { ex.Message };
						page.OnWorkerTaskUpdate.Invoke(this);
						return;
					}

					break;
				case UniTaskStatus.Succeeded:
					Logger.LogDebug("WorkerTask: result is succeeded");
					break;
				case UniTaskStatus.Pending: // hum ?
				default:
					Status      = WorkerTaskStatus.CompletedWithoutResult;
					MessageKey  = "search.worker.no_message";
					MessageArgs = Array.Empty<string>();
					page.OnWorkerTaskUpdate.Invoke(this);
					return;
			}

			Result = await result;
			_t1    = DateTime.Now;
			Logger.LogDebug($"WorkerTask: {(_t1 - _t0).TotalMilliseconds:0}ms");

			if (Result == null) {
				Status      = WorkerTaskStatus.Faulted;
				MessageKey  = "search.worker.error.no_result";
				MessageArgs = Array.Empty<string>();
				page.OnWorkerTaskUpdate.Invoke(this);
				return;
			}

			if (Result.IsError) {
				Status      = WorkerTaskStatus.Faulted;
				MessageKey  = "search.worker.error";
				MessageArgs = new[] { Result.Error };
				page.OnWorkerTaskUpdate.Invoke(this);
				return;
			}

			var data = Result.Data;
			if (data == null || data.Length == 0) {
				Status      = WorkerTaskStatus.CompletedWithoutResult;
				MessageKey  = "search.worker.empty";
				MessageArgs = Array.Empty<string>();
				page.OnWorkerTaskUpdate.Invoke(this);
				return;
			}

			Status      = WorkerTaskStatus.Completed;
			MessageKey  = "search.worker.no_message";
			MessageArgs = Array.Empty<string>();
			page.OnWorkerTaskUpdate.Invoke(this);
		}
	}
}