﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;


namespace TAlex.Common.Diagnostics.ErrorReporting
{
    public class ErrorReportSender : IErrorReportSender
    {
        #region IErrorReportSender Members

        public void Send(ErrorReportModel report)
        {
            SendReport(CreateRequest(), SerializeReport(report));
        }

        #endregion

        #region Methods

        private HttpWebRequest CreateRequest()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://talex-soft.com/api/ErrorReporting/Send");
            request.Method = "POST";
            request.ContentType = "application/json";

            return request;
        }

        private void SendReport(HttpWebRequest request, byte[] bytes)
        {
            request.ContentLength = bytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            CheckResponse(request);
        }

        private byte[] SerializeReport(ErrorReportModel report)
        {
            var serializedReport = new JavaScriptSerializer().Serialize(report);
            return Encoding.UTF8.GetBytes(serializedReport);
        }

        private void CheckResponse(HttpWebRequest request)
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException(response.StatusDescription);
                }
            }
        }

        #endregion
    }
}