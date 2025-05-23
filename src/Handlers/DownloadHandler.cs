﻿using CefSharp;
using SharpBrowser.Managers;

namespace SharpBrowser.Handlers {
	internal class DownloadHandler : IDownloadHandler {
		readonly MainForm myForm;

		public DownloadHandler(MainForm form) {
			myForm = form;
		}

		//
		// Summary:
		//     Called before a download begins in response to a user-initiated action (e.g.
		//     alt + link click or link click that returns a `Content-Disposition: attachment`
		//     response from the server).
		//
		// Parameters:
		//   chromiumWebBrowser:
		//     the ChromiumWebBrowser control
		//
		//   browser:
		//     The browser instance
		//
		//   url:
		//     is the target download URL
		//
		//   requestMethod:
		//     is the target method (GET, POST, etc)
		//
		// Returns:
		//     Return true to proceed with the download or false to cancel the download.
		public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod) {
			return true;
		}

		//
		// Summary:
		//     Called before a download begins.
		//
		// Parameters:
		//   chromiumWebBrowser:
		//     the ChromiumWebBrowser control
		//
		//   browser:
		//     The browser instance
		//
		//   downloadItem:
		//     Represents the file being downloaded.
		//
		//   callback:
		//     Callback interface used to asynchronously continue a download.

		public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem item, IBeforeDownloadCallback callback) {
			if (!callback.IsDisposed) {
				using (callback) {

					DownloadManager.UpdateDownloadItem(item);

					// ask browser what path it wants to save the file into
					string path = DownloadManager.CalcDownloadPath(item);

					// if file should not be saved, path will be null, so skip file
					if (path == null) {

						// skip file
						callback.Continue(path, false);
						return false;

					}
					else {

						// open the downloads tab
						myForm.OpenDownloads();
						callback.Continue(path, true);
						return true;
					}

				}
			}
			return false;
		}

		//
		// Summary:
		//     Called when a download's status or progress information has been updated. This
		//     may be called multiple times before and after CefSharp.IDownloadHandler.OnBeforeDownload(CefSharp.IWebBrowser,CefSharp.IBrowser,CefSharp.DownloadItem,CefSharp.IBeforeDownloadCallback).
		//
		// Parameters:
		//   chromiumWebBrowser:
		//     the ChromiumWebBrowser control
		//
		//   browser:
		//     The browser instance
		//
		//   downloadItem:
		//     Represents the file being downloaded.
		//
		//   callback:
		//     The callback used to Cancel/Pause/Resume the process
		public void OnDownloadUpdated(IWebBrowser webBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback) {
			DownloadManager.UpdateDownloadItem(downloadItem);
			if (downloadItem.IsInProgress && DownloadManager.CancelRequests.Contains(downloadItem.Id)) {
				callback.Cancel();
			}
			//Console.WriteLine(downloadItem.Url + " %" + downloadItem.PercentComplete + " complete");
		}

	}
}