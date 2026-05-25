namespace Nox.Search.Runtime.Clients {
	internal enum WorkerTaskStatus {
		Pending,
		Fetching,
		Canceled,
		CompletedWithoutResult,
		Completed,
		Faulted
	}
}