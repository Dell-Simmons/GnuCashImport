using System.Net.Http.Headers;
namespace ShippoMailingLabels
{
    public class ShippoMailingLabelClient
    {
        public async Task PullShippoMailingLabelPurchases(string key)
        {
            var myClient = new HttpClient();

            myClient.BaseAddress = new Uri(@"https://api.goshippo.com/");

            myClient.DefaultRequestHeaders.Clear();
            myClient.DefaultRequestHeaders.Add("Authorization", "ShippoToken " + key);

            var targetEndPoint = "transactions?results=199";


           // var stringPayload = JsonConvert.SerializeObject(_dcLabelRequest);

            // Wrap our JSON inside a StringContent which then can be used by the HttpClient class
            //var httpContent = new StringContent(stringPayload, System.Text.Encoding.UTF8, "application/json");
            var response = myClient.GetAsync(targetEndPoint).Result;
            //  errorsContainer = null;
            if (response.IsSuccessStatusCode)
            {
                var returnString = response.Content.ReadAsStringAsync().Result;

                //var successResponse = JsonConvert.DeserializeObject<CreateDCLabelResponse>(
                //    response.Content.ReadAsStringAsync().Result);
               // return successResponse;
            }
            else
            {
                var returnString = response.Content.ReadAsStringAsync().Result;
                //var errorResponse =
                //       JsonConvert.DeserializeObject<CreateDCLabelResponse>(response.Content.ReadAsStringAsync().Result);
                //errorsContainer = errorResponse;

              //  return errorResponse;
            }





            //client.DefaultRequestHeaders.Authorization =
            //new AuthenticationHeaderValue("ShippoToken", key);

            //var response = await client.GetAsync(
            //    "https://api.goshippo.com/transactions/?results=100");

            //string json = await response.Content.ReadAsStringAsync();
        }
    }
}
