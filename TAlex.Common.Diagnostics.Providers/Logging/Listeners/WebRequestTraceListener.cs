﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using System.Xml.XPath;
using TAlex.Common.Diagnostics.Models;


namespace TAlex.Common.Diagnostics.Logging.Listeners
{
    public class WebRequestTraceListener : XmlWriterTraceListener
    {
        public WebRequestTraceListener(string fileName)
            : base(fileName)
        {
        }


        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            base.TraceData(eventCache, source, eventType, id, ToXPathNavigator(data, source));
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            base.TraceData(eventCache, source, eventType, id, data.Select(x => ToXPathNavigator(x, source)).ToArray());
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            TraceData(eventCache, source, eventType, id);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            TraceData(eventCache, source, eventType, id, message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            TraceData(eventCache, source, eventType, id, String.Format(format, args));
        }


        private object ToXPathNavigator(object data, string source)
        {
            TraceRecord record = new TraceRecord();

            if (HttpContext.Current != null)
                FillRecord(record, HttpContext.Current);

            if (data is Exception)
            {
                record.Exception = new ExceptionInfo((Exception)data);
                record.Description = record.Exception.Message;
            }
            else
            {
                record.Description = data + String.Empty;
            }
            if (String.IsNullOrWhiteSpace(record.Description))
            {
                record.Description = source;
            }

            XmlSerializer ser = new XmlSerializer(record.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                ser.Serialize(stream, record);
                stream.Position = 0;

                var doc = new XPathDocument(stream);
                return doc.CreateNavigator();
            }
        }


        private void FillRecord(TraceRecord record, HttpContext context)
        {
            try
            {
                var request = context.Request;

                record.RequestUrl = request.Url + String.Empty;
                record.UserAgent = request.UserAgent;
                record.UrlReferrer = request.UrlReferrer + String.Empty;
                record.HttpMethod = request.HttpMethod;
                record.PostData = CreateQueryString(request.Form);
                record.Status = GetStatus(context);
                record.Handler = context.Handler + String.Empty;
                record.UserName = context.User != null ? context.User.Identity.Name : String.Empty;
                record.UserHostAddress = request.UserHostAddress;
            }
            catch (HttpException)
            {
            }
        }

        private string CreateQueryString(NameValueCollection vals)
        {
            return String.Join("&", vals.Keys.Cast<string>().Select(x => String.Format("{0}={1}", x, vals[x])));
        }

        private string GetStatus(HttpContext context)
        {
            HttpException exc = context.Error as HttpException;

            if (exc != null)
                return exc.GetHttpCode().ToString();
            else if (context.Error != null)
                return "500 Server Error";

            return context.Response.Status;
        }
    }
}
