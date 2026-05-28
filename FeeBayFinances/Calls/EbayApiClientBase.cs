using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FeeBayFinances.Calls
{
    public abstract class EbayApiClientBase
    {
        protected readonly HttpClient HttpClient;
        private readonly IEbayTokenService _tokenService;

        protected abstract string BaseUrl { get; }

        protected EbayApiClientBase(
            HttpClient httpClient,
            IEbayTokenService tokenService)
        {
            HttpClient = httpClient;
            _tokenService = tokenService;
        }

        protected async Task<T> GetAsync<T>(string relativeUrl)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{BaseUrl}/{relativeUrl}");

            await AddAuthorizationAsync(request);

            return await SendAsync<T>(request);
        }

        protected async Task<TResponse> PostAsync<TRequest, TResponse>(
            string relativeUrl,
            TRequest body)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{BaseUrl}/{relativeUrl}");

            await AddAuthorizationAsync(request);

            var json = JsonSerializer.Serialize(body);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            return await SendAsync<TResponse>(request);
        }

        protected async Task DeleteAsync(string relativeUrl)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{BaseUrl}/{relativeUrl}");

            await AddAuthorizationAsync(request);

            await SendAsync(request);
        }

        private async Task AddAuthorizationAsync(HttpRequestMessage request)
        {
            var token = await _tokenService.GetValidUserAccessTokenAsync();

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<T> SendAsync<T>(HttpRequestMessage request)
        {
            using var response = await HttpClient.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw CreateEbayException(response, json);
            }

            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonSerializer.Deserialize<T>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        private async Task SendAsync(HttpRequestMessage request)
        {
            using var response = await HttpClient.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw CreateEbayException(response, json);
            }
        }

        private Exception CreateEbayException(
            HttpResponseMessage response,
            string responseBody)
        {
            return new Exception(
                $"eBay API Error {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}\n{responseBody}");
        }
    }
}
