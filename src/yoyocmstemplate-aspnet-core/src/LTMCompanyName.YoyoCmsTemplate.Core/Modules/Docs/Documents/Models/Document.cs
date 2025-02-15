﻿using System.Collections.Generic;

namespace LTMCompanyName.YoyoCmsTemplate.Modules.yoyo.Docs.Documents.Models
{
    public class Document
    {
        public string Title { get; set; }

        public string Content { get; set; }

        public string Format { get; set; }

        public string EditLink { get; set; }

        public string RootUrl { get; set; }

        public string RawRootUrl { get; set; }

        public string Version { get; set; }

        public string LocalDirectory { get; set; }

        public string FileName { get; set; }
        public List<DocumentContributor> Contributors { get; set; }

        public bool SuccessfullyRetrieved { get; set; }
    }
}
