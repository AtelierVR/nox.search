namespace api.nox.search.client {
	internal enum WorkerTaskStatus {
		Pending,
		Fetching,
		Canceled,
		CompletedWithoutResult,
		Completed,
		Faulted
	}
}