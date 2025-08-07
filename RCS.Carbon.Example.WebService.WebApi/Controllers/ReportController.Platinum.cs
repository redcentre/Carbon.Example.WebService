using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using RCS.Azure.Data.Processor;

namespace RCS.Carbon.Example.WebService.WebApi.Controllers;

partial class ReportController
{
	/// <summary>
	/// This method runs on a long-lived Thread to allow multiple parallel Platinum report filter and
	/// formats to happen in the background of the web services lifetime. Technically, a web worker should
	/// be used for this, but it might be overkill until a real need for that arises. The method must not
	/// access anything in the request-response pipline for its lifetime, because one is unlikely to exist
	/// by the time the thread starts. All the session related values are passed in through the state parameter.
	/// </summary>
	async void PlatinumBatchProc(object? state)
	{
		var data = (BatchData)state!;
		data.StartedEvent.Set();
		data.Response.StartTime = DateTime.UtcNow;
		var po = new ParallelOptions { MaxDegreeOfParallelism = data.Response.ParallelMax, CancellationToken = data.Cts.Token };
		try
		{
			await Parallel.ForAsync(0, data.Request.ReportNames.Length, po, async (ix, ct) =>
			{
				var report = data.Response.Reports![ix];
				report.StartTime = DateTime.UtcNow;
				using (var wrap = new StateWrap(data.SessionId, LicProv, false))
				{
					try
					{
						wrap.Engine.TableLoadCBT(report.Name);
						var dprops = wrap.Engine.Job.DisplayTable.DisplayProps;
						wrap.Engine.QuickEdit(dprops.Cells.Frequencies.Visible, dprops.Cells.ColumnPercents.Visible, dprops.Cells.RowPercents.Visible, dprops.Significance.Visible, data.Request.Filter);
						var platdata = wrap.Engine.TableAsPlatinum();
						if (data.Request.SaveAsBlobs)
						{
							string json = JsonSerializer.Serialize(platdata);
							var sp = new StorageProcessor(data.StorageConnect!, data.ContainerName!);
							// The generated report name will be a long concatenation of most of the values being
							// passed in here (see StorageProcessor.MakeReportBC). The session Id changes too often
							// and will result in too many report blobs being created, so the account user Id is used
							// in its place to make more stable names. This many cause a problem if the same user is
							// running multiple batches that include the same reports simultaneously, which is unlikely
							// enough to be ignored for now.
							var blob = await sp.GetStreamForReport(data.UserId, data.CustomerName, data.JobName, report.Name + ".json", PlatinumbatchVDirPrefix, "application/json", (stream) =>
							{
								JsonSerializer.Serialize(stream, platdata);
							});
							data.Response.Reports[ix].BlobUri = blob.Uri.AbsoluteUri;
						}
						else
						{
							// The full reports are put into an array old holding properties and
							// aren't seen by the client until the batch is complete and they will
							// all be returned in the response report array.
							data.HoldDatas[ix] = platdata;
						}
					}
					catch (Exception ex)
					{
						report.FailureType = ex.GetType().Name;
						var errors = new List<string>();
						var e = ex;
						while (e != null)
						{
							errors.Add(e.Message);
							e = e.InnerException;
						}
						report.FailureMessages = [.. errors];
						//report.FailureMessages = [ex.ToString()];		// <========= FOR DUMPING THE FULL ERROR IN DEBUGGING =========
					}
				}
				report.ElapsedSeconds = DateTime.UtcNow.Subtract(report.StartTime.Value).TotalSeconds;
			});
		}
		catch (OperationCanceledException canex)
		{
			// If the parallel async loop is cancelled, all running delegates will run to completion
			// but no more are scheduled, then this Exception will be caught. In this case, some reports
			// may remain in the waiting state even though the batch is completed.
			Trace.WriteLine($"Expected {canex.Message}");
			data.Response.IsCancelled = true;
		}
		// Move the possibly large amount of report data from the holding array back to the
		// response reports so the client can get all the data once the batch is complete.
		if (!data.Request.SaveAsBlobs)
		{
			for (int i = 0; i < data.Response.Reports!.Length; i++)
			{
				data.Response.Reports[i].Data = data.HoldDatas[i];
			}
		}
		data.Response.ElapsedSeconds = DateTime.UtcNow.Subtract(data.Response.StartTime.Value).TotalSeconds;
	}
}
