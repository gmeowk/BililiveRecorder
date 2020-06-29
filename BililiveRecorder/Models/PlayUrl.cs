using System;
using System.Collections.Generic;
using System.Text;

namespace BililiveRecorder.Models
{
    public class Quality_descriptionItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int qn { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string desc { get; set; }
    }

    public class DurlItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int length { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int order { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int stream_type { get; set; }
    }

    public class PlayUrlData
    {
        /// <summary>
        /// 
        /// </summary>
        public int current_quality { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> accept_quality { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int current_qn { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<Quality_descriptionItem> quality_description { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<DurlItem> durl { get; set; }
    }

    public class PlayUrl
    {
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ttl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PlayUrlData data { get; set; }
    }
}
