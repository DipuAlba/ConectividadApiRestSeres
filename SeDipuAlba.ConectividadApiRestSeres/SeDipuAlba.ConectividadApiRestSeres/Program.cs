using Microsoft.Extensions.Configuration;
using RestSharp;

//CONFIGURACIÓN
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var wsClave = configuration["WsPassword"];

Console.WriteLine("Calculamos hash...");

const string nifTramitador = "00000000T";
const string entidad = "26071"; 
const string wsUsuario = "seres3_p";
const string endPointUrl = $"https://pre-{entidad}.sedipualba.es/seres/api/";
var ahora = DateTime.UtcNow;

//CÁLCULO DEL HASH
//El código del cálculo del hash está en la clase SeDipuAlba.Artilugios.HashedPassword (https://github.com/DipuAlba/Artilugios/blob/main/SeDipuAlba.Artilugios/HashedPassword.cs)
var calculadoraHash = new SeDipuAlba.Artilugios.HashedPassword(wsClave);
var hash = calculadoraHash.Generate(ahora);

Console.WriteLine($"El hash para «{wsClave}» es «{hash}» a las «{ahora}».");


//LLAMADAS A LA API USANDO UN CLIENTE GENERADO CON NSWAG
Console.WriteLine($"Probamos cliente Nswag.");
Console.WriteLine($"Generamos JWT con cliente Nswag...");
using var httpClientJwt = new HttpClient()
{
    BaseAddress = new Uri(endPointUrl + "jwtautenticacion")
};

var clienteNswagJwt = new ClienteNswag.Client(httpClientJwt);
var jwt = await clienteNswagJwt.ApiJwtAuthentication_PostValueAsync(new ClienteNswag.ParametrosAutenticacion()
    {
        WsEntidad = entidad,
        NifTramitador = nifTramitador,
        WsSegPassword = hash,
        WsSegUser = wsUsuario
    });

Console.WriteLine($"JWT obtenido: {jwt}");

Console.WriteLine($"Probamos cliente Nswag");
using var httpClientCiudadanos = new HttpClient()
{
    BaseAddress = new Uri(endPointUrl + "ciudadanos")
};

httpClientCiudadanos.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
var clienteNswag = new ClienteNswag.Client(httpClientCiudadanos);

var listado = await clienteNswag.ApiCiudadanos_ListProcedenciasAsync();
Console.WriteLine($"Listado obtenido con {listado.Count} elementos.");

//LLAMADAS A LA API USANDO RestSharp
Console.WriteLine($"Probamos cliente RestSharp.");
Console.WriteLine($"Generamos JWT con RestSharp...");
var jwtClient = new RestClient(endPointUrl + "jwtautenticacion");
var jwtRequest = new RestRequest("", Method.Post)
    .AddJsonBody(new
    {
        WsEntidad = entidad,
        NifTramitador = nifTramitador,
        WsSegPassword = hash,
        WsSegUser = wsUsuario
    });

var jwtResponse = await jwtClient.ExecuteAsync(jwtRequest);
if (!jwtResponse.IsSuccessful)
{
    Console.WriteLine($"Error obteniendo JWT: {jwtResponse.StatusCode} - {jwtResponse.ErrorMessage}");
    return;
}
jwt = jwtResponse.Content?.Trim('"'); // Si el JWT viene como string entre comillas

Console.WriteLine($"JWT obtenido: {jwt}");

var ciudadanosClient = new RestClient(endPointUrl + "ciudadanos");
var ciudadanosRequest = new RestRequest("listprocedencias", Method.Get);
ciudadanosRequest.AddHeader("Authorization", $"Bearer {jwt}");

var ciudadanosResponse = await ciudadanosClient.ExecuteAsync(ciudadanosRequest);
if (!ciudadanosResponse.IsSuccessful)
{
    Console.WriteLine($"Error obteniendo listado: {ciudadanosResponse.StatusCode} - {ciudadanosResponse.ErrorMessage}");
    return;
}

var listadoRestSharp = System.Text.Json.JsonSerializer.Deserialize<List<object>>(ciudadanosResponse.Content ?? "[]");
Console.WriteLine($"Listado obtenido con {listado?.Count ?? 0} elementos.");