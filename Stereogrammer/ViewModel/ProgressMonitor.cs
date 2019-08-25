using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.ComponentModel;
using System.Diagnostics;

namespace Stereogrammer.ViewModel
{
    public class ProgressMonitor :IDisposable
    {
        public delegate float UpdateProgress();
        public delegate void ReportStatus( double progress );
        public delegate void OnStart();
        public delegate void OnCompletion();

        UpdateProgress _updateProgress = null;
        ReportStatus _reportStatus;
        OnStart _onStart;
        OnCompletion _onCompletion;

        double progress = 0.0;

        BackgroundWorker monitor;

        /// <summary>
        /// Create a progress monitor with delegates for reporting status and completion
        /// </summary>
        /// <param name="report"></param>
        /// <param name="complete"></param>
        public ProgressMonitor( ReportStatus report, OnStart start = null, OnCompletion complete = null )
        {
            _reportStatus = report;
            _onStart = start;
            _onCompletion = complete;

            monitor = new BackgroundWorker();
            monitor.DoWork += new DoWorkEventHandler( monitor_DoWork );
            monitor.RunWorkerCompleted += new RunWorkerCompletedEventHandler( monitor_Completed );
            monitor.ProgressChanged += new ProgressChangedEventHandler( monitor_ReportProgress );
            monitor.WorkerReportsProgress = true;
            monitor.WorkerSupportsCancellation = true;
        }

        public void Dispose()
        {
            monitor.Dispose();
        }

        /// <summary>
        /// Start monitoring a process
        /// </summary>
        /// <param name="update"></param>
        public void MonitorProgress( UpdateProgress update )
        {
            Debug.Assert( update != null );
            _updateProgress = update;

            if ( _onStart != null )
            {
                _onStart();
            }

            if ( false == monitor.IsBusy )
            {
                monitor.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Stop monitoring a process
        /// </summary>
        public void EndMonitoring()
        {
            if ( monitor.IsBusy )
            {
                monitor.CancelAsync();
                _updateProgress = null;
            }
        }

        private void monitor_DoWork( object sender, DoWorkEventArgs e )
        {
            BackgroundWorker monitor = (BackgroundWorker)sender;

            while ( !monitor.CancellationPending && _updateProgress != null )
            {
                monitor.ReportProgress(0);

                Thread.Sleep( 50 );
            }
        }

        private void monitor_ReportProgress( object sender, ProgressChangedEventArgs e )
        {
            if ( _updateProgress != null )
            {
                progress = _updateProgress();

                if ( _reportStatus != null )
                {
                    _reportStatus( progress );
                }                
            }
        }

        private void monitor_Completed( object sender, RunWorkerCompletedEventArgs e )
        {
            if ( _onCompletion != null )
            {
                _onCompletion();
            }
        }
    }
}
