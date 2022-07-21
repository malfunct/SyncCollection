using System;
using System.Collections.Generic;

namespace SyncCollection
{
    class InternetArchiveSearchResult
    {
        public ResponseHeader responseHeader { get; set; }
        public Response response { get; set; }
    }

    public class Params
    {
        public string q { get; set; }
        public string qin { get; set; }
        public string fl { get; set; }
        public string wt { get; set; }
        public string sort { get; set; }
        public string rows { get; set; }
        public int start { get; set; }
    }

    public class ResponseHeader
    {
        public int status { get; set; }
        public int QTime { get; set; }
        public Params @params { get; set; }
    }

    public class Doc
    {
        public string identifier { get; set; }
        public List<DateTime> oai_updatedate { get; set; }
    }

    public class Response
    {
        public int numFound { get; set; }
        public int start { get; set; }
        public List<Doc> docs { get; set; }
    }
}
