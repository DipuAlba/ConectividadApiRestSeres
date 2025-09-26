using Microsoft.Extensions.Configuration;

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

var calculadoraHash = new SeDipuAlba.Artilugios.HashedPassword(wsClave);
var hash = calculadoraHash.Generate(ahora);

Console.WriteLine($"El hash para «{wsClave}» es «{hash}» a las «{ahora}».");

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

