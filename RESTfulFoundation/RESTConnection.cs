using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RESTfulFoundation
{

    /// <summary>
    /// The 
    /// </summary>
    public class RESTConnection
    {
        public string RootPath { get; private set; }
        public List<string>? Info { get; private set; }

        private string agent;
        private static readonly CookieContainer cookies = new();
        private static readonly HttpClientHandler handler = new() { CookieContainer = cookies };
        private static readonly HttpClient httpClient = new(handler);

        /// <summary>
        /// The only constructor on the Connection, a rootPath to a RESTful
        /// interface needs to be provided and is required.  
        /// </summary>
        /// <param name="rootPath">The root to the RESTful service, used for all
        ///     resource calls within the scope of the connection.</param>
        /// <param name="userAgent">An optional name for the client that will
        ///     be presented to the server as the User-Agent string in every
        ///     request procesed.</param>
        public RESTConnection(string rootPath, string? userAgent = null)
        {
            RootPath = rootPath;
            Info = null;
            agent = (userAgent != null) ? userAgent : "RESTfulFoundation.Client";

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Add("User-Agent", agent);
        }

        /// <summary>
        /// Takes that passed in string paramters and uses them to build a URL
        /// string
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        public string BuildUrlString(params string[] parts)
        {
            string result = RootPath + (RootPath.EndsWith("/") ? "" : "/");

            foreach (string s in parts)
            {
                result += (s.StartsWith("/") ? s.Substring(1) : s);
                result += (result.EndsWith("/") ? "" : "/");
            }

            return result;
        }

        /// <summary>
        /// The List method is essentially a 'get' request with no parameters
        /// that expects the service to rerturn an array of objects that can be
        /// used to navigate into the tree.
        ///
        /// NOTE:
        ///     If the call fails, it will return null, and populate the Info
        ///     property that will list any and all conditions that failed in
        ///     processing.
        /// </summary>
        /// <typeparam name="T">A scoped result that is derived from the RESTObject
        ///     class</typeparam>
        /// <param name="path">the portion of the URL that follows the RootPath
        ///     on the connection</param>
        /// <param name="page"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        public RESTObjectList<T>? List<T>(string path,
            long? page = null, int? perPage = null)
            where T : RESTObject
        {
            var task = Task.Run(async () => await ListAsync<T>(path, page, perPage));
            return task.Result;
        }

        /// <summary>
        /// The ListAsyn method is essentially a 'get' request with no required
        /// parameters that expects the service to rerturn an array of objects
        /// that can be used to navigate into the tree.
        ///
        /// NOTE:
        ///     If the call fails, it will return null, and populate the Info
        ///     property that will list any and all conditions that failed in
        ///     processing.
        /// </summary>
        /// <typeparam name="T">A scoped result that is derived from the RESTObject
        ///     class</typeparam>
        /// <param name="path">the portion of the URL that follows the RootPath
        ///     on the connection</param>
        /// <param name="page"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        public async Task<RESTObjectList<T>?> ListAsync<T>(
            string path, long? page = null, int? perPage = null) where T : RESTObject
        {
            if (Info != null) Info = null;

            try
            {
                string url = BuildUrlString(path);

                var streamTask = httpClient.GetStreamAsync(url);

                if (streamTask is null) {
                    Info = new();
                    Info.Add("Unable to obtain a valid stream");
                    return null;
                }

                StreamReader reader = new StreamReader(await streamTask);
                string content = await reader.ReadToEndAsync();

                RESTObjectList<T>? result = null;

                // is this an object, or an array?
                if (content.StartsWith("["))
                {
                    // this is an array
                    List<T>? array = JsonSerializer.Deserialize<List<T>>(content);
                    if (array == null)
                    {
                        Info = new();
                        Info.Add("Unable to process a list from the result");
                        return null;
                    }
                    result = new RESTObjectList<T>(array!);
                    return result;
                }

                result = JsonSerializer.Deserialize<RESTObjectList<T>>(content);
                // 

                if (result == null)
                {
                    Info = new();
                    Info.Add("No List Returned");
                    return null;
                }
                return result!;
            }
            catch (Exception error)
            {
                Info = new();
                Info.Add(string.Format("An Exception was raised: {0}", error.Message));
                return null;
            }
        }

        /// <summary>
        /// The Query method is internally a post to a /query/ path on the
        /// api.  It takes a model of the request object with the values to
        /// search set on it, and then returns a list of objects that match the
        /// passed in criteria.  Wildcards should be supported in the passed in
        /// criteria, however this is *server* dependant.
        ///
        /// NOTE:
        ///     If the call fails, it will return null, and populate the Info
        ///     property that will list any and all conditions that failed in
        ///     processing.
        /// </summary>
        /// <typeparam name="T">A scoped result that is derived from the RESTObject
        ///     class</typeparam>
        /// <param name="path">the portion of the URL that follows the RootPath
        ///     on the connection</param>
        /// <param name="page"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        public RESTObjectList<T>? Query<T>(
            string path,
            T criteria,
            long page = 0,
            long perPage = 0) where T : RESTObject
        {
            var task = Task.Run(async () => await QueryAsync<T>(path, criteria, page, perPage));
            return task.Result;
        }

        /// <summary>
        /// The QueryAsync method is internally a post to a /query/ path on the
        /// api.  It takes a model of the request object with the values to
        /// search set on it, and then returns a list of objects that match the
        /// passed in criteria.  Wildcards should be supported in the passed in
        /// criteria, however this is *server* dependant.
        ///
        /// NOTE:
        ///     If the call fails, it will return null, and populate the Info
        ///     property that will list any and all conditions that failed in
        ///     processing.
        /// </summary>
        /// <typeparam name="T">A scoped result that is derived from the RESTObject
        ///     class</typeparam>
        /// <param name="path">the portion of the URL that follows the RootPath
        ///     on the connection</param>
        /// <param name="page"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        public async Task<RESTObjectList<T>?> QueryAsync<T>(
            string path,
            T criteria,
            long page = 0,
            long perPage = 0) where T : RESTObject
        {
            try
            {
                string url = BuildUrlString(path, "/query/");

                // serialize the model to send in the body
                string modelData = JsonSerializer.Serialize(criteria);

                var buffer = System.Text.Encoding.UTF8.GetBytes(modelData);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await httpClient.PostAsync(url, byteContent);
                response.EnsureSuccessStatusCode();
                RESTObjectList<T>? result = await JsonSerializer.DeserializeAsync<RESTObjectList<T>>(await response.Content.ReadAsStreamAsync());
                if (result == null)
                {
                    Info = new();
                    Info.Add("No List Returned");
                    return null;
                }

                return result!;
            }
            catch (Exception error)
            {
                Info = new();
                Info.Add(string.Format("An Exception was raised: {0}", error.Message));
                return null;
            }
        }

        /// <summary>
        /// The foundational Get request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public T? Get<T>(
            string path,
            string? id = null) where T : RESTObject?
        {
            var task = Task.Run(async () => await GetAsync<T>(path, id));
            return task.Result;
        }

        /// <summary>
        /// The foundational GetAsync request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<T?> GetAsync<T>(
            string path,
            string? id = null) where T : RESTObject?
        {
            if (Info != null) Info = null;

            try
            {
                string url = BuildUrlString(path, id ?? "");

                var streamTask = httpClient.GetStreamAsync(url);

                if (streamTask is null)
                {
                    Info = new();
                    Info.Add("Unable to obtain a valid stream");
                    return null;
                }

                T? result = await JsonSerializer.DeserializeAsync<T?>(await streamTask);
                if (result == null)
                {
                    Info = new();
                    Info.Add("No Object Returned");
                    return null;
                }

                return result;
            }
            catch (Exception error)
            {
                Info = new();
                Info.Add(string.Format("An Exception was raised: {0}", error.Message));
                return null;
            }
        }

        public T? Postc<T, U>(
            string path,
            U model) where T : RESTObject? where U : RESTObject?
        {
            var task = Task.Run(async () => await PostAsync<T, U>(path, model));
            return task.Result;
        }

        public async Task<T?> PostAsync<T, U>(
            string path,
            U model) where T : RESTObject? where U : RESTObject?
        {
            try
            {
                string url = BuildUrlString(path);

                // serialize the model to send in the body
                string modelData = JsonSerializer.Serialize(model);

                var buffer = System.Text.Encoding.UTF8.GetBytes(modelData);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await httpClient.PostAsync(url, byteContent);
                response.EnsureSuccessStatusCode();
                T? result = await JsonSerializer.DeserializeAsync<T?>(await response.Content.ReadAsStreamAsync());
                if (result == null)
                {
                    Info = new();
                    Info.Add("No Object Returned");
                    return null;
                }

                return result;
            }
            catch (Exception error)
            {
                Info = new();
                Info.Add(string.Format("An Exception was raised: {0}", error.Message));
                return null;
            }
        }
        public T? Put<T, U>(
            string path,
            string id,
            U model) where T : RESTObject? where U : RESTObject?
        {
            var task = Task.Run(async () => await PutAsync<T, U>(path, id, model));
            return task.Result;
        }

        public async Task<T?> PutAsync<T, U>(
            string path,
            string id,
            U model) where T : RESTObject? where U : RESTObject?
        {
            try
            {
                string url = BuildUrlString(path, id ?? "");

                // serialize the model to send in the body
                string modelData = JsonSerializer.Serialize(model);

                var buffer = System.Text.Encoding.UTF8.GetBytes(modelData);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await httpClient.PutAsync(url, byteContent);
                response.EnsureSuccessStatusCode();
                T? result = await JsonSerializer.DeserializeAsync<T?>(await response.Content.ReadAsStreamAsync());
                if (result == null)
                {
                    Info = new();
                    Info.Add("No Object Returned");
                    return null;
                }

                return result;
            }
            catch (Exception error)
            {
                Info = new();
                Info.Add(string.Format("An Exception was raised: {0}", error.Message));
                return null;
            }
        }


               

        public Boolean Delete(
            string path,
            string id)
        {
            var task = Task.Run(async () => await DeleteAsync(path, id));
            return task.Result;
        }

        public async Task<Boolean> DeleteAsync(
            string path,
            string id)
        {
            try
            {
                string url = BuildUrlString(path, id ?? "");
                HttpResponseMessage response = await httpClient.DeleteAsync(url);
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch (Exception error)
            {
                Info = new();
                Info.Add(string.Format("An Exception was raised: {0}", error.Message));
                return false;
            }
        }
    }
}