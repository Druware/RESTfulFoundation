using System;
using System.Text.Json.Serialization;

namespace RESTfulFoundation
{
    /// <summary>
    /// Class RESTObjectList implements a pageable ( optionally ) implementation
    /// of a result list. By default, .NET implements a robust IList<> generic
    /// for a JSON Array, however, that interface does not readily allow for
    /// paging, so this exists.  In most implementations, this list result would
    /// be a top level object result containing an array rather than an array
    /// found as a child of another object.
    /// </summary>
    public class RESTObjectList<T> : RESTObject where T : RESTObject
    {
        public RESTObjectList() { }

        public RESTObjectList(IList<T> items)
        {
            List = (List<T>?)items;
            TotalRecords = List!.Count;
        }

        [JsonPropertyName("totalRecords")]
        public Int64? TotalRecords { get; set; }

        [JsonPropertyName("page")]
        public Int64? Page { get; set; }

        [JsonPropertyName("perPage")]
        public Int64? PerPage { get; set; }

        [JsonPropertyName("list")]
        public List<T>? List { get; set; } // I think I am going to have to deal with this manually...
    }
}

