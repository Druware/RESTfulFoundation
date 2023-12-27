/* This file is part of the RESTfulFoundation Library
 *
 * The RESTfulFoundation Library is free software: you can redistribute it
 * and/or modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version.
 *
 * The RESTfulFoundation is distributed in the hope that it will be
 * useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
 * Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along with
 * the RESTfulFoundation Library. If not, see
 * <https://www.gnu.org/licenses/>.
 *
 * Copyright 2019-2024 by:
 *    Satori & Associates, Inc.
 *    All Rights Reserved
 ******************************************************************************/

/* History
 *   Modified By  How
 *   -------- --- --------------------------------------------------------------
 *   23/10/25 ars migrated from internal library code to something that we will
 *                make widely available as the code matures
 *   23/12/26 ars major code cleanup and restructure to provide better error
 *                handling and reporting
 ******************************************************************************/

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RESTfulFoundation;


public enum RESTConnectionRequestMethod
{
    Get,
    Put,
    Post,
    Delete
}

/// <summary>
/// the Connection object represents the root of all client/server transactions
/// within the Client API. It encapsulates the internal methods in both
/// synchronous and asynchronous models.
/// </summary>
public class RESTConnection
{
    private const string JsonDecodingError = "Unable to decode result";
    
    #region Properties 
    
    /// <summary>
    /// set at creation, this is the root base URL of the API instance being
    /// called throughout the lifecycle.  This cannot be changed as it would
    /// invalidate the security and state management
    /// </summary>
    private readonly string _rootPath;

    /// <summary>
    /// the Info property will contain any additional information from the
    /// last action on the connection object, and *should* be contextual to
    /// that action
    /// </summary>
    public List<string>? Info { get; private set; }

    /// <summary>
    /// internal cookie storage
    /// </summary>
    private static readonly CookieContainer Cookies = new();

    /// <summary>
    /// internal reference to the underlying http handler
    /// </summary>
    private static readonly HttpClientHandler Handler = new()
        { CookieContainer = Cookies };

    /// <summary>
    /// internal reference to the underlying http client connection
    /// </summary>
    private static readonly HttpClient HttpClient = new(Handler);

    #endregion

    /// <summary>
    /// Constructor for the Connection.  Most applications should only use one
    /// connection throughout their lifecycle.
    /// </summary>
    /// <param name="rootPath"></param>
    public RESTConnection(string rootPath)
    {
        _rootPath = rootPath;
        Info = null;

        HttpClient.DefaultRequestHeaders.Accept.Clear();
        HttpClient.DefaultRequestHeaders.Add("User-Agent",
            "RESTfulFoundation.Client");
    }

    #region Internal Utility Methods
    
    /// Builds a URL from string parts, appended to the internal rootPath that
    /// was established upon creation of the Connection object.
    ///
    /// - parameter parts: an array of the string parts to be appended to the
    ///                    path
    /// - returns: the resulting string, with properly formed elements using a
    ///            "/" delimiter
    ///
    /// # Notes: #
    /// * At this point, the code works, but will probably be identified to have
    ///   issues once evaluated more fully.
    ///
    /// # Example #
    /// ```
    /// let conn = Connection(basePath: "http://localhost")
    /// let url = conn.buildUrlString(parts: "api/controller/", "itemId")
    /// // resulting url = "http://localhost/api/controller/itemId"
    /// print(url)
    /// ```
    public static string BuildUrlString(string? queryString = null, params string[] parts)
    {
        var result = "";
        foreach (var part in parts)
        {
            if (part == "") continue;
            result += part.StartsWith("/") ? part.Substring(1) : part;
            result += result.EndsWith("/") ? "" : "/";

        }
        result += result.EndsWith("/") ? "" : "/";

        if (queryString != "")
            result += $"?{queryString}";
        
        return result;
    }

    private void SetInfo(params string[] info)
    {
        // if the Info is currently blank, create it
        Info ??= new List<string>();
        foreach (var item in info)
            Info?.Add(item);
    }

    private void ResetInfo() => Info = null;

    private async Task<Stream?> DoRequest(string url, string? body = null, RESTConnectionRequestMethod method = RESTConnectionRequestMethod.Get)
    {
        ResetInfo();
        
        try
        {
            ByteArrayContent? byteContent = null;

            if (body != null)
            {
                var buffer = System.Text.Encoding.UTF8.GetBytes(body);
                byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType =
                    new MediaTypeHeaderValue("application/json");
            }

            HttpResponseMessage? response;
            switch (method)
            {
                case RESTConnectionRequestMethod.Delete:
                    response = await HttpClient.DeleteAsync(url);
                    break;
                case RESTConnectionRequestMethod.Get:
                    response = await HttpClient.GetAsync(url);
                    break;
                case RESTConnectionRequestMethod.Put:
                    if (byteContent == null)
                    {
                        SetInfo("Body cannot be Null or Empty");
                        return null;
                    }
                    response = await HttpClient.PutAsync(url, byteContent);
                    break;
                case RESTConnectionRequestMethod.Post:
                    if (byteContent == null)
                    {
                        SetInfo("Body cannot be Null or Empty");
                        return null;
                    }
                    response = await HttpClient.PostAsync(url, byteContent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }

            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStreamAsync();
            
            // not successful, so handle the result.
            SetInfo(await response.Content.ReadAsStringAsync());
            response.Content.Dispose();
            return null;

        }
        catch (Exception error)
        {
            SetInfo(error.Message);
            if (error.InnerException != null) SetInfo($"Additional Information: {error.InnerException.Message}");
        }
        return null;
    }

    #endregion
    
    /// <summary>
    /// implements a simple 'get' call to any api end point, usually wrapped by
    /// the client objects whenever appropriate.
    /// </summary>
    /// <param name="path">path to the endpoint</param>
    /// <param name="id">optionally, an id referencing a specific entity in the
    ///     collection</param>
    /// <param name="queryString"></param>
    /// <param name="completion">optionally, a closure for on completion</param>
    /// <param name="failure">optional closure for handling error or failure
    ///     conditions</param>
    /// <typeparam name="T">An RESTObject based type</typeparam>
    /// <returns>An RESTObject based type</returns>
    public async Task<T?> GetAsync<T>(
        string path,
        string? id = null,
        string? queryString = null,
        Action<T?>? completion = null,
        Action<string?>? failure = null) where T : RESTObject?
    {
        var url = BuildUrlString(queryString, _rootPath, path, id ?? "");
        try
        {
            var result = await JsonSerializer.DeserializeAsync<T?>(await DoRequest(url) ?? throw new InvalidOperationException());
            
            if (result == null)
            {
                failure?.Invoke(JsonDecodingError);
                SetInfo(JsonDecodingError);
                return null;
            }
            
            completion?.Invoke(result);
            return result;
        }
        catch (Exception error)
        {
            if (failure == null) return null;
            SetInfo(error.Message);
            if (error.InnerException != null) SetInfo($"Additional Information: {error.InnerException.Message}");
            failure(error.Message);
        }
        return null;
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
    /// the list command calls a 'get' on the root, with no id, and no default
    /// parameters in order to get a list ( per the normal REST conventions )
    /// on an endpoint.
    /// 
    /// There are two variations, one returns a pageable 'RESTObjectList',
    /// while the other returns an array of object
    /// </summary>
    /// <param name="path">path to the endpoint</param>
    /// <param name="perPage">number of items per page</param>
    /// <param name="queryString">defines any queryString params needed</param>
    /// <param name="completion">optionally, a closure for on completion</param>
    /// <param name="failure">optional closure for handling error or failure
    ///     conditions</param>
    /// <param name="page">Zero based page number in the result set</param>
    /// <typeparam name="T">An RESTObject based type</typeparam>
    /// <returns>An RESTObject based type</returns>
    public async Task<RESTObjectList<T>> ListAsync<T>(
        string path,
        int page, 
        int perPage = 100,
        string queryString = "",
        Action<RESTObjectList<T>?>? completion = null,
        Action<string?>? failure = null) where T : RESTObject
    {
        var query = queryString;
        query += query.Length == 0 ? "" : "&";
        query += $"page={page}&perPage={perPage}";
        
        var url = BuildUrlString(query, _rootPath, path);
        try
        {
            var result = await JsonSerializer.DeserializeAsync<RESTObjectList<T>?>(await DoRequest(url) ?? 
                throw new InvalidOperationException());
            if (result == null)
                failure?.Invoke("No Object Returned");
            else
                completion?.Invoke(result);
            return result ?? new RESTObjectList<T>();
        }
        catch (Exception error)
        {
            SetInfo(error.Message);
            if (error.InnerException != null) SetInfo($"Additional Information: {error.InnerException.Message}");
            failure?.Invoke(error.Message);
        }
        return new RESTObjectList<T>();
    }
    
    public RESTObjectList<T> List<T>(string path, int page = 0, int perPage = 100, string queryString = "")
        where T : RESTObject
    {
        var task = Task.Run(async () => await ListAsync<T>(path, page, perPage, queryString));
        return task.Result;
    }
    
    /// <summary>
    /// the array command calls a 'get' on the root, with no id, and no default
    /// parameters in order to get a list ( per the normal REST conventions )
    /// on an endpoint.
    /// 
    /// There are two variations, one returns a pageable 'RESTObjectList',
    /// while the other returns an array of object
    /// </summary>
    /// <param name="path"></param>
    /// <param name="queryString"></param>
    /// <param name="completion"></param>
    /// <param name="failure"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>an array of T or an empty array on failure</returns>
    public async Task<T[]?> ListAsync<T>(
        string path,
        string queryString = "",
        Action<T[]?>? completion = null,
        Action<string?>? failure = null) where T : RESTObject
    {
        var url = BuildUrlString(queryString, _rootPath, path);
        try
        {
            var result = await JsonSerializer.DeserializeAsync<T[]?>(await DoRequest(url) ?? 
                                                                     throw new InvalidOperationException());
            if (result == null)
                failure?.Invoke("No Object Returned");
            else
                completion?.Invoke(result);
            return result ?? Array.Empty<T>();
        }
        catch (Exception error)
        {
            SetInfo(error.Message);
            if (error.InnerException != null) 
                    SetInfo($"Additional Information: {error.InnerException.Message}");
            failure?.Invoke(error.Message);
        }
        return Array.Empty<T>();
    }

    /// <summary>
    /// using a posted model object as the search criteria, run the query
    /// against the server objects and return a pageable result of the
    /// resulting list.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="queryString"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T[]? List<T>(string path, string queryString = "") where T : RESTObject
    {
        var task = Task.Run(async () => await ListAsync<T>(path, queryString));
        return task.Result;
    }
    
    /// <summary>
    /// using a posted model object as the search criteria, run the query
    /// against the server objects and return a pageable result of the
    /// resulting list.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="criteria"></param>
    /// <param name="page"></param>
    /// <param name="perPage"></param>
    /// <param name="completion"></param>
    /// <param name="failure"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<RESTObjectList<T>?> QueryAsync<T>(
        string path,
        T criteria,
        long page = 0,
        long perPage = 0,
        Action<RESTObjectList<T>?>? completion = null,
        Action<string?>? failure = null) where T : RESTObject
    {
        try
        {
            var url = BuildUrlString($"?page={page}&perPage={perPage}", _rootPath, path, (path.EndsWith("/") ? "query/" : "/query/"));
            var result = await JsonSerializer.DeserializeAsync<RESTObjectList<T>?>(
                await DoRequest(url, JsonSerializer.Serialize(criteria)) ?? throw new InvalidOperationException());
            if (result == null)
            {
                failure?.Invoke("No Object Returned");
                return new RESTObjectList<T>();
            }

            completion?.Invoke(result);
            return result;
        }
        catch (Exception error)
        {
            if (failure == null) return null;
            SetInfo(error.Message);
            if (error.InnerException != null) SetInfo($"Additional Information: {error.InnerException.Message}");
            failure(error.Message);
        }

        return null;
    }
    
    /// <summary>
    /// using a posted model object as the search criteria, run the query
    /// against the server objects and return a pageable result of the
    /// resulting list.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="criteria"></param>
    /// <param name="page"></param>
    /// <param name="perPage"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public RESTObjectList<T>? Query<T>(
        string path,
        T criteria,
        long page = 0,
        long perPage = 0) where T : RESTObject
    {
        var task = Task.Run(async () => await QueryAsync(path, criteria, page, perPage));
        return task.Result;
    }

    /// <summary>
    /// Post will push a new record of the type in the model to the server, and assuming a successful push return the
    /// newly posted record back to the client
    /// </summary>
    /// <param name="path"></param>
    /// <param name="model"></param>
    /// <param name="completion"></param>
    /// <param name="failure"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TU"></typeparam>
    /// <returns></returns>
    public async Task<T?> PostAsync<T, TU>(
        string path,
        TU model,
        Action<T?>? completion = null,
        Action<string?>? failure = null)
        where T : RESTObject? where TU : RESTObject?
    {
        try
        {
            var url = BuildUrlString(null, _rootPath, path);
            var result = await JsonSerializer.DeserializeAsync<T?>(
                await DoRequest(url, JsonSerializer.Serialize(model), RESTConnectionRequestMethod.Post) 
                    ?? throw new InvalidOperationException());
            if (result == null)
            {
                failure?.Invoke("No Object Returned");
                return null;
            }

            completion?.Invoke(result);
            return result;
        }
        catch (Exception error)
        {
            if (failure == null) return null;
            SetInfo(error.Message);
            if (error.InnerException != null) SetInfo($"Additional Information: {error.InnerException.Message}");
            failure(error.Message);
        }

        return null;
    }
    
    /// <summary>
    /// Post will push a new record of the type in the model to the server, and assuming a successful push return the
    /// newly posted record back to the client
    /// </summary>
    /// <param name="path"></param>
    /// <param name="model"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TU"></typeparam>
    /// <returns></returns>
    public T? Post<T, TU>(
        string path,
        TU model) where T : RESTObject? where TU : RESTObject?
    {
        var task = Task.Run(async () => await PostAsync<T, TU>(path, model));
        return task.Result;
    }

    /// <summary>
    /// Put will push an update record of the type in the model to the server, to the Id reflected in the Id record.
    /// Assuming a successful push return the newly updated record back to the client
    /// </summary>
    /// <param name="path"></param>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="completion"></param>
    /// <param name="failure"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TU"></typeparam>
    /// <returns></returns>
    public async Task<T?> PutAsync<T, TU>(
        string path,
        string id,
        TU model,
        Action<T?>? completion = null,
        Action<string?>? failure = null)
        where T : RESTObject? where TU : RESTObject?
    {
        try
        {
            var url = BuildUrlString(null, _rootPath, path, (path.EndsWith("/") ? "" : "/") + id);
            var result = await JsonSerializer.DeserializeAsync<T?>(
                await DoRequest(url, JsonSerializer.Serialize(model), RESTConnectionRequestMethod.Put) 
                ?? throw new InvalidOperationException());
            if (result == null)
            {
                failure?.Invoke("No Object Returned");
                return null;
            }
            completion?.Invoke(result);
            return result;

        }
        catch (Exception error)
        {
            if (failure == null) return null;
            SetInfo(error.Message);
            if (error.InnerException != null) SetInfo($"Additional Information: {error.InnerException.Message}");
            failure(error.Message);
        }

        return null;
    }

    /// <summary>
    /// Put will push an update record of the type in the model to the server, to the Id reflected in the Id record.
    /// Assuming a successful push return the newly updated record back to the client
    /// </summary>
    /// <param name="path"></param>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TU"></typeparam>
    /// <returns></returns>
    public T? Put<T, TU>(
        string path,
        string id,
        TU model) where T : RESTObject? where TU : RESTObject?
    {
        var task = Task.Run(async () => await PutAsync<T, TU>(path, id, model));
        return task.Result;
    }
    
    /// <summary>
    /// Delete will request the removal of the record identified by the passed in Id, and will return a true or false
    /// reflecting the success or failure of the request
    /// </summary>
    /// <param name="path"></param>
    /// <param name="id"></param>
    /// <param name="completion"></param>
    /// <param name="failure"></param>
    /// <returns></returns>
    public async Task<bool> DeleteAsync(
        string path,
        string id,
        Action<bool>? completion = null,
        Action<string?>? failure = null)
    {
        try
        {
            var url = BuildUrlString(null, _rootPath, path,
                (path.EndsWith("/") ? "" : "/") + id);
            var response = await HttpClient.DeleteAsync(url);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                completion?.Invoke(true);
                return true;
            }
            SetInfo(response.StatusCode.ToString());
            failure?.Invoke("Request Returned an error: " + response.StatusCode);
            return false;
        }
        
        catch (Exception error)
        {
            if (failure == null) return false;
            SetInfo(error.Message);
            if (error.InnerException != null) SetInfo($"Additional Information: {error.InnerException.Message}");
            failure(error.Message);
        }

        return false;
    }
    
    /// <summary>
    /// Delete will request the removal of the record identified by the passed in Id, and will return a true or false
    /// reflecting the success or failure of the request
    /// </summary>
    /// <param name="path"></param>
    /// <param name="id"></param>
    /// <returns>true or false reflecting the success or failure</returns>
    public bool Delete(
        string path,
        string id)
    {
        var task = Task.Run(async () => await DeleteAsync(path, id));
        return task.Result;
    }
}