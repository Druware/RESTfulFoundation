using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RESTfulFoundation
{
    public class ArrayOrObjectJsonConverter<T> : JsonConverter<IReadOnlyCollection<T>>
    {
        public override IReadOnlyCollection<T>? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.StartArray => JsonSerializer.Deserialize<T[]>(ref reader, options),
                JsonTokenType.StartObject => JsonSerializer.Deserialize<Wrapper>(ref reader, options)?.Items,
                _ => throw new JsonException()
            };

        public override void Write(Utf8JsonWriter writer, IReadOnlyCollection<T> value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, (object?)value, options);

        private record Wrapper(T[] Items);
    }


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




        public RESTObjectList<T>? List<T>(string path,
            long? page = null, int? perPage = null)
            where T : RESTObject
        {
            var task = Task.Run(async () => await ListAsync<T>(path, page, perPage));
            return task.Result;
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
        public async Task<RESTObjectList<T>?> ListAsync<T> (
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

                // first we try this as an api object with a list, then we try as
                // an array..

                var options = new JsonSerializerOptions();
                options.Converters.Add(new ArrayOrObjectJsonConverter<T>());

                IReadOnlyCollection<T>? list = await
                    JsonSerializer.DeserializeAsync<IReadOnlyCollection<T>>(await streamTask);


                RESTObjectList<T>? result = new RESTObjectList<T>(list!);
                //    await JsonSerializer.DeserializeAsync<RESTObjectList<T>>(await streamTask);


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
        /*
                /// <summary>
                /// The foundational Get request.
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <param name="path"></param>
                /// <param name="id"></param>
                /// <param name="completion"></param>
                /// <param name="failure"></param>
                /// <returns></returns>
                public async Task<T?> Get<T>(
                    string path,
                    string id,
                    Action<T?>? completion = null,
                    Action<string?>? failure = null) where T : RESTObject?
                {
                    try
                    {
                        string url =
                            Base +
                        path +
                            ((path.EndsWith("/") ? "" : "/") +
                            ((id != null) ? id.Trim() : ""));

                        var streamTask = httpClient.GetStreamAsync(url);
                        if (streamTask is null) { return null; }
                        T? result = await JsonSerializer.DeserializeAsync<T?>(await streamTask);
                        if (result == null)
                        {
                            if (failure != null)
                            {
                                failure("No Object Returned");
                                return null;
                            }
                        }

                        if (completion != null)
                        {
                            completion(result);
                            return result;
                        }
                        return result;
                    }
                    catch (Exception error)
                    {
                        if (failure != null)
                        {
                            failure(error.Message);
                            return null;
                        }
                    }
                    return null;
                }

                public async Task<APIList<T>> List<T>(
                    string path,
                    Action<APIList<T>?>? completion = null,
                    Action<string?>? failure = null) where T : APIObject
                {
                    try
                    {
                        string url =
                        Base +
                        path +
                        ((path.EndsWith("/") ? "" : "/"));

                        var streamTask = httpClient.GetStreamAsync(url);
                        if (streamTask is null) { return new APIList<T>(); }
                        APIList<T>? result = await JsonSerializer.DeserializeAsync<APIList<T>>(await streamTask);
                        if (result == null)
                        {
                            if (failure != null)
                            {
                                failure("No Object Returned");
                                return new APIList<T>(); ;
                            }
                        }

                        if (completion != null)
                        {
                            completion(result!);
                            return result!;
                        }
                        return result!;
                    }
                    catch (Exception error)
                    {
                        if (failure != null)
                        {
                            failure(error.Message);
                            return new APIList<T>(); ;
                        }
                    }
                    return new APIList<T>(); ;
                }

                public async Task<APIList<T>?> Query<T>(
                string path,
                    T criteria,
                    long page = 0,
                    long perPage = 0,
                    Action<APIList<T>?>? completion = null,
                    Action<string?>? failure = null) where T : APIObject
                {
                    try
                    {
                        string url =
                            Base +
                            path +
                            ((path.EndsWith("/") ? "query/" : "/query/"));

                        // serialize the model to send in the body
                        string modelData = JsonSerializer.Serialize(criteria);

                        var buffer = System.Text.Encoding.UTF8.GetBytes(modelData);
                        var byteContent = new ByteArrayContent(buffer);
                        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                        HttpResponseMessage response = await httpClient.PostAsync(url, byteContent);
                        response.EnsureSuccessStatusCode();
                        APIList<T>? result = await JsonSerializer.DeserializeAsync<APIList<T>>(await response.Content.ReadAsStreamAsync());
                        if (result == null)
                        {
                            if (failure != null)
                                failure("No Object Returned");
                            return new();
                        }

                        if (completion != null)
                            completion(result!);
                        return result!;
                    }
                    catch (Exception error)
                    {
                        if (failure != null)
                            failure(error.Message);
                        return new();
                    }
                }

                public async Task<T?> Post<T, U>(
                    string path,
                    U model,
                    Action<T?>? completion = null,
                    Action<string?>? failure = null) where T : APIObject? where U : APIObject?
                {
                    try
                    {
                        string url = Base + path;

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
                            if (failure != null)
                            {
                                failure("No Object Returned");
                                return null;
                            }
                        }

                        if (completion != null)
                        {
                            completion(result);
                            return result;
                        }
                        return result;
                    }
                    catch (Exception error)
                    {
                        if (failure != null)
                        {
                            failure(error.Message);
                            return null;
                        }
                    }
                    return null;
                }

                public async Task<T?> Put<T, U>(
                    string path,
                    string id,
                    U model,
                    Action<T?>? completion = null,
                    Action<string?>? failure = null) where T : APIObject? where U : APIObject?
                {
                    try
                    {
                        string url = Base + path + ((path.EndsWith("/") ? "" : "/") + id);

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
                            if (failure != null)
                            {
                                failure("No Object Returned");
                                return null;
                            }
                        }

                        if (completion != null)
                        {
                            completion(result);
                            return result;
                        }
                        return result;
                    }
                    catch (Exception error)
                    {
                        if (failure != null)
                        {
                            failure(error.Message);
                            return null;
                        }
                    }
                    return null;
                }

                public async Task<Boolean> Delete(
                    string path,
                    string id,
                    Action<Boolean>? completion = null,
                    Action<string?>? failure = null)
                {
                    try
                    {
                        string url = Base + path + ((path.EndsWith("/") ? "" : "/") + id);

                        HttpResponseMessage response = await httpClient.DeleteAsync(url);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (completion != null)
                                completion(true);
                            return true;
                        }

                        if (failure != null)
                            failure("Request Returned an error: " + response.StatusCode.ToString());
                        return false;
                    }
                    catch (Exception error)
                    {
                        if (failure != null)
                        {
                            failure(error.Message);
                            return false;
                        }
                    }
                    return false;
                }
                */
    }
}