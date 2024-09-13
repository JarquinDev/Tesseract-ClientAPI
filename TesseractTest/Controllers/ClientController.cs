using Microsoft.AspNetCore.Mvc;
using TesseractTest.db;
using TesseractTest.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Tesseract;
using Microsoft.VisualBasic;

namespace TesseractTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController:ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetClientes()
        {
            return await _context.Clientes.ToListAsync();
        }

        // GET: api/Clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
            {
                return NotFound();
            }

            return cliente;
        }
        //Endpoint to register a new client with an imagae of costarrican ID
        [HttpPost("register-client-from-image")]
        public async Task<IActionResult> RegisterClientFromImage (IFormFile image)
        {
            if (image == null)
            {
                return BadRequest("No Image upload");
            }
            string extractedText;

            //Temporarily save the image on the server
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            try
            {
                using (var engine = new TesseractEngine(@"./tessdata", "spa", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(tempFilePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            extractedText = page.GetText();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {

                return StatusCode(500, $"Error processing image: {ex.Message}");
            }

            try
            {
                //validate if the image is a costarrican ID
                bool isCedula = IsValidCedula(extractedText);
                if (isCedula)
                {
                    //Extract {name,lastname1,lastname2 and ID from the text extracted from the image}
                    var info = dataCed(extractedText);

                    // Check if the cedula is already registered
                    var existingClient = await _context.Clientes.FirstOrDefaultAsync(c => c.Cedula == info.Cedula);

                    if (existingClient != null)
                    {
                        // If cedula is already registered, return a message
                        return Conflict(new
                        {
                            message = "this client is already registered",
                            client = existingClient
                        });
                    }


                    //Add the extracted information (client) to the database context
                    _context.Clientes.Add(info);

                    // Save the changes asynchronously into the database
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Cliente registrado con éxito",
                        client = info
                    });
                }
                else
                {
                    return Ok(new { validation = "Error consulting data" });
                }
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error processing image: {ex.Message}");
            }
            finally
            {// Delete the temporarily file 
                System.IO.File.Delete(tempFilePath);
            }
        }
        [HttpPost("get-client")]
        public async Task<IActionResult> GetClients()
        {

            try
            {   
               //get all data from table clients
                var clients = await _context.Clientes.ToListAsync();
               //verify if table client is empty
                if (!clients.Any())
                {
                    return NotFound("The list is empty");
                }
                else 
                {
                    return Ok(clients);
                }
            }
            catch (Exception ex)
            {

                // Return a 500 error if something goes wrong
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPut("update-client/{id}")]
        public async Task<IActionResult> UpdateClient (int id, [FromBody] Client updatedclient)
        {
            try
            {
                // Check if the ID provided in the URL matches the ID in the updated client object
                if (id != updatedclient.Id)
                {
                    // If they don't match, return a 400 Bad Request indicating the mismatch
                    return BadRequest("Client id mismatch");
                }
                Console.WriteLine(id);
                // Search for the client in the database by the provided ID
                var client = await _context.Clientes.FindAsync(id);

                // If the client is not found in the database, return a 404 Not Found
                if (client == null)
                { 
                    return NotFound("Client not Found");
                }
                // Update the client properties with the values from the updated client object
                client.Name = updatedclient.Name;
                client.Lastname1 = updatedclient.Lastname1;
                client.Lastname2 = updatedclient.Lastname2;
                client.Cedula = updatedclient.Cedula;

                try
                {
                    // Save the changes asynchronously to the database
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {

                    // Return a 500 error if there's a concurrency issue during the update
                    return StatusCode(500, "Error updating the client");
                }
                // Return the updated client object with a 200 OK status
                return Ok(client);
            }
            catch (Exception ex)
            {

                return StatusCode(500,ex);
            }
        }
        [HttpDelete("delete-client")]
        public async Task<IActionResult> DeleteClientWithImage(IFormFile image)
        {
            if (image == null)
            {
                return BadRequest("No Image upload");
            }
            string extractedText;

            //Temporarily save the image on the server
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            try
            {
                using (var engine = new TesseractEngine(@"./tessdata", "spa", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(tempFilePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            extractedText = page.GetText();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {

                return StatusCode(500, $"Error processing image: {ex.Message}");
            }
            try
            {
                //get the cedula number from the text extracted from the image
                string numced = numCed(extractedText);
                
                //get the client by the cedula number
                var client = await _context.Clientes.FirstOrDefaultAsync(c => c.Cedula == numced);
                
                // If the client is not found in the database, return a 404 Not Found
                if (client == null)
                {
                    return NotFound("Client not found");
                }
                else
                {
                    //delete the client 
                    _context.Clientes.Remove(client);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

                return StatusCode(500,ex.Message);
            }
            return Ok("Client deleted");
        }
        private bool IsValidCedula(string text)
        {
            text = CleanText(text);
            var cedulaPattern = @"\b\d{1}\s\d{4}\s\d{4}\b";
            var cedulaMatch = Regex.Match(text, cedulaPattern);
            return Regex.IsMatch(text, cedulaPattern);
        }
        private string numCed(string text)
        {
            var cedulaPattern = @"\b\d{1}\s\d{4}\s\d{4}\b";
            var cedulaMatch = Regex.Match(text, cedulaPattern);
            text = cedulaMatch.ToString();
            return text;
        }
        private Client dataCed(string text)
        {
            // Define a regex pattern to match the "Name" section in the text
            var namePattern = @"Nombre[:\s]*([A-Za-z\s]+)";
            // Define a regex pattern to match the Costa Rican ID format (cedula)
            var cedulaPattern = @"\b\d{1}\s\d{4}\s\d{4}\b";
            // Define a regex pattern to match the first last name (1%Apellido)
            var firstLastName = @"1[%][\s]*[Aa]pellido[:\s]*([\w]+)";
            // Define a regex pattern to match the second last name (2 followed by any number and "Apellido")
            var secondLastName = @"2\d+[\s\W]*[Aa]pellido[:\s]*([A-Za-z]+)(?=\W|$)";

            // Extract the "cedula" (Costa Rican ID) from the text using the cedula pattern
            var cedulaMatch = Regex.Match(text, cedulaPattern);
            string cedula = cedulaMatch.ToString();

            // Extract the first last name using the first last name pattern
            var firstLastNameMatch = Regex.Match(text, firstLastName, RegexOptions.IgnoreCase);
            var lastname = firstLastNameMatch.Success ? CleanText(firstLastNameMatch.Groups[1].Value.Trim()) : null;
            string lastname1 = lastname.ToString();

            // Extract the second last name using the second last name pattern
            var secondLastNameMatch = Regex.Match(text, secondLastName, RegexOptions.IgnoreCase);
            var secondlast = secondLastNameMatch.Success ? CleanText(secondLastNameMatch.Groups[1].Value.Trim()) : null;
            string lastname2 = secondlast.ToString();

            // Extract the name using the name pattern
            var namematch = Regex.Match(text, namePattern, RegexOptions.IgnoreCase);
            var Name = namematch.Success ? CleanText(namematch.Groups[1].Value.Trim()) : null;
            string name = Name.ToString();

            // Return a new Client object with the extracted values for Name, Lastname1, Lastname2, and Cedula
            return new Client
            {
                Name = name,
                Lastname1 = lastname1,
                Lastname2 = lastname2,
                Cedula = cedula
            };
        }
        private string CleanText(string text)
        {
            // delete especial characters
            text = Regex.Replace(text, @"[\r\n]+", " ");
            text = Regex.Replace(text, @"\s+", " ");

            // Remove sequences that contain numbers followed by a special character or a letter after a '/'.
            text = Regex.Replace(text, @"/\d+\w", "");
            text = Regex.Replace(text, @"/\w+", "");

            // clean no needed patreon
            text = Regex.Replace(text, @"\d[\s°%]*[Aa]pellido", "Apellido");
            text = text.Trim();
            return text;
        }
    }
}
