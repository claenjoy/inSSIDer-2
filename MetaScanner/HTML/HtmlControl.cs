﻿////////////////////////////////////////////////////////////////

#region Header

//
// Copyright (c) 2007-2010 MetaGeek, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion Header


////////////////////////////////////////////////////////////////
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using inSSIDer.Properties;

namespace inSSIDer.HTML
{
    public partial class HtmlControl : WebBrowser
    {
        #region Fields

        private string _analyticsSource = string.Empty;
        private bool _firstPageLoaded;
        private string _updateUrl = string.Empty;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Google Analytics Source parameter to attach to all web links
        /// </summary>
        [Category("Data")]
        public string AnalyticsSource
        {
            get { return _analyticsSource; }
            set { _analyticsSource = value; }
        }

        private string LocalFileName
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        [Category("Behavior")]
        public bool OpenWebLinks
        {
            get; set;
        }

        /// <summary>
        /// If set to 0, then never update
        /// </summary>
        [Category("Data")]
        public float UpdateIntervalDays
        {
            get; set;
        }

        [Category("Data")]
        public string UpdateUrl
        {
            get { return _updateUrl; }
            set { _updateUrl = value; }
        }

        #endregion Properties

        #region Constructors

        public HtmlControl()
        {
            LocalFileName = Path.GetTempPath() + "MetaGeekNews" + Path.DirectorySeparatorChar + "news.html";

            InitializeComponent();
        }

        #endregion Constructors

        #region Public Methods

        public bool UpdateFile(bool forceUpdate)
        {
            bool displayingFile = true;

            //string htmlFile = Path.GetTempPath() + "MetaGeekNews" + Path.DirectorySeparatorChar + "news.html";

            if ((UpdateIntervalDays > 0) && (UpdateUrl != string.Empty))
            {

                // Skip if it's not time to update the file yet
                if (forceUpdate || (DateTime.Now - File.GetLastWriteTime(LocalFileName)).TotalDays > UpdateIntervalDays)
                {
                    string rssFile = Path.ChangeExtension(LocalFileName, "rss");

                    try
                    {

                        // BackgroundWorker runs UpdateFile()
                        // and then runs RunWorkerCompleted()
                        BackgroundWorker bw = new BackgroundWorker();
                        bw.RunWorkerCompleted += (s, e) =>
                                                     {
                                                         int count = 0;
                                                         Stop();
                                                         while (IsBusy && count < 4)
                                                         {
                                                             //Simply to prevent high CPU usage.
                                                             Thread.Sleep(300);
                                                             Stop();
                                                             count++;
                                                         }
                                                         if (e.Error == null && count < 4)
                                                         {
                                                             //Refresh();
                                                             Navigate(LocalFileName);
                                                         }
                                                     };
                        bw.DoWork += (s, e) => Download.UpdateFile(rssFile, UpdateUrl);
                        bw.RunWorkerAsync();
                    }
                    catch (COMException)
                    {
                        // thrown on some machines.. no good solution found online...
                        displayingFile = false;
                    }
                }
            }

            return displayingFile;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// First document won't load until about:blank has loaded
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDocumentCompleted(WebBrowserDocumentCompletedEventArgs e)
        {
            base.OnDocumentCompleted(e);

            if (!_firstPageLoaded)
            {
                _firstPageLoaded = true;
                OpenLocalFile();
            }
        }

        protected override void OnNavigating(WebBrowserNavigatingEventArgs e)
        {
            base.OnNavigating(e);

            // open links in real browser
            if (!OpenWebLinks && (!e.Url.AbsolutePath.Equals("blank") && e.Url.Scheme.Equals("http")))
            {
                LinkHelper.OpenLink(e.Url.ToString(), Settings.Default.AnalyticsMedium, AnalyticsSource);
                e.Cancel = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        ///// <summary>
        ///// 
        ///// </summary>
        //[Category("Data")]
        //public string LocalFileName
        //{
        //    get { return _localFileName; }
        //    set
        //    {
        //        _localFileName = value;
        //        // load the file if the browser is already initialized
        //        if (_firstPageLoaded)
        //            OpenLocalFile();
        //    }
        //}

        private void OpenLocalFile()
        {
            // strip anchors, etc. from any HTTP-path style local file names
            string strippedPath = LocalFileName;

            // removing anchors
            int hashIndex = LocalFileName.IndexOf('#');
            if (hashIndex > 0)
                strippedPath = LocalFileName.Remove(hashIndex);

            // loading for first time so set the page to the LocalFileName
            if (File.Exists(strippedPath))
                Navigate(Path.GetFullPath(LocalFileName));
        }

        #endregion Private Methods
    }
}