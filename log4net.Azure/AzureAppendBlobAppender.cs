﻿using System;
using System.IO;
using System.Text;
using System.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using log4net.Appender.Extensions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using log4net.Appender.Language;
using log4net.Core;
using log4net.Layout;

namespace log4net.Appender
{
    public class AzureAppendBlobAppender : BufferingAppenderSkeleton
    {
        private CloudStorageAccount _account;
        private CloudBlobClient _client;
        private CloudBlobContainer _cloudBlobContainer;

        public string ConnectionStringName { get; set; }
        private string _connectionString;
        private string _lineFeed = "";

        public string ConnectionString
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ConnectionStringName))
                {
                    var config = ConfigurationManager.ConnectionStrings[ConnectionStringName];
                    if (config != null)
                        return config.ConnectionString;
                }
                if (String.IsNullOrEmpty(_connectionString))
                    throw new ApplicationException(Resources.AzureConnectionStringNotSpecified);
                return _connectionString;
            }
            set
            {
                _connectionString = value;
            }
        }

        private string _containerName;

        public string ContainerName
        {
            get
            {
                if (String.IsNullOrEmpty(_containerName))
                    throw new ApplicationException(Resources.ContainerNameNotSpecified);
                return _containerName;
            }
            set
            {
                _containerName = value;
            }
        }

        private string _directoryName;

        public string DirectoryName
        {
            get
            {
                if (String.IsNullOrEmpty(_directoryName))
                    throw new ApplicationException(Resources.DirectoryNameNotSpecified);
                return _directoryName;
            }
            set
            {
                _directoryName = value;
            }
        }

        private string _filenamePrefixDateTimeFormat;

        public string FileNamePrefixDateTimeFormat
        {
            get
            {
                if (String.IsNullOrEmpty(_filenamePrefixDateTimeFormat))
                    _filenamePrefixDateTimeFormat = "yyyy_MM_dd";
                return _filenamePrefixDateTimeFormat;
            }
            set
            {
                _filenamePrefixDateTimeFormat = value;
            }
        }

        private string _filenameSuffix;

        public string FileNameSuffix
        {
            get
            {
                if (String.IsNullOrEmpty(_filenameSuffix))
                    _filenameSuffix = ".entry.log";
                return _filenameSuffix;
            }
            set
            {
                _filenameSuffix = value;
            }
        }

        /// <summary>
        /// Sends the events.
        /// </summary>
        /// <param name="events">The events that need to be send.</param>
        /// <remarks>
        /// <para>
        /// The subclass must override this method to process the buffered events.
        /// </para>
        /// </remarks>
        protected override void SendBuffer(LoggingEvent[] events)
        {
            CloudAppendBlob appendBlob = _cloudBlobContainer.GetAppendBlobReference(Filename(_directoryName, _filenamePrefixDateTimeFormat, _filenameSuffix));
            if (!appendBlob.Exists()) appendBlob.CreateOrReplace();
            else _lineFeed = Environment.NewLine;

            foreach (var e in events)
            {
                ProcessEvent(e);
            }
        }

        private void ProcessEvent(LoggingEvent loggingEvent)
        {
            CloudAppendBlob appendBlob = _cloudBlobContainer.GetAppendBlobReference(Filename(_directoryName, _filenamePrefixDateTimeFormat, _filenameSuffix));

            PatternLayout layout = (PatternLayout)base.Layout;
            string log = layout.Format(loggingEvent);
            
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(log)))
            {
                appendBlob.AppendBlock(ms);
            }
        }

        private static string Filename(string directoryName, string filenamePrefixDateTimeFormat, string filenameSuffix)
        {
            return string.Format("{0}/{1}{2}",
                                 directoryName,
                                 DateTime.Today.ToString(filenamePrefixDateTimeFormat, DateTimeFormatInfo.InvariantInfo),
                                 filenameSuffix);
        }

        /// <summary>
        /// Initialize the appender based on the options set
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is part of the <see cref="T:log4net.Core.IOptionHandler"/> delayed object
        ///             activation scheme. The <see cref="M:log4net.Appender.BufferingAppenderSkeleton.ActivateOptions"/> method must 
        ///             be called on this object after the configuration properties have
        ///             been set. Until <see cref="M:log4net.Appender.BufferingAppenderSkeleton.ActivateOptions"/> is called this
        ///             object is in an undefined state and must not be used. 
        /// </para>
        /// <para>
        /// If any of the configuration properties are modified then 
        ///             <see cref="M:log4net.Appender.BufferingAppenderSkeleton.ActivateOptions"/> must be called again.
        /// </para>
        /// </remarks>
        public override void ActivateOptions()
        {

            base.ActivateOptions();

            _account = CloudStorageAccount.Parse(ConnectionString);
            _client = _account.CreateCloudBlobClient();
            _cloudBlobContainer = _client.GetContainerReference(ContainerName.ToLower());
            _cloudBlobContainer.CreateIfNotExists();
            
        }
    }
}
