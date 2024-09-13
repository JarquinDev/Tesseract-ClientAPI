using Microsoft.AspNetCore.Mvc;
using TesseractTest.db;
using TesseractTest.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Tesseract;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Xml.Linq;
using Google.Protobuf.WellKnownTypes;

namespace TesseractTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        [HttpPost("extract-text")]
        public async Task<IActionResult> ExtractTextFromImage(IFormFile image)
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

            //extract text from the Image using tesseract
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
            finally
            {// Delete the temporarily file 
                System.IO.File.Delete(tempFilePath);
            }
            return Ok(new { text = extractedText });
        }
        [HttpPost ("validate-cedula")]
        public async Task<IActionResult> ValidateCedula(IFormFile image)
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

            //extract text from the Image using tesseract
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
                bool isCedula = IsValidCedula(extractedText);
                Console.WriteLine(isCedula);

                if (isCedula) 
                {
                    
                    string numced = numCed(extractedText);
                    return Ok(new {
                        validation = "Accepted, it is a cédula",
                        text = numced, 
                        isCedula=true
                        });
                }
                else 
                {
                    return Ok(new { validation ="Denied,it is not a cédula" ,text = extractedText, isCedula = false });
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

        [HttpPost("extract-ced-Info")]
        public async Task<IActionResult> Extract_Ced_information (IFormFile image)
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
                bool isCedula = IsValidCedula(extractedText);
                if (isCedula)
                {
                    string numced = numCed(extractedText);
                    var info = dataCed(extractedText);
                    return Ok(new
                    {
                        data = info
                    });
                }
                else
                {
                    return Ok(new { validation = "Error consulting data"});
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


        

        private bool IsValidCedula(string text) {
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
            var namePattern = @"Nombre[:\s]*([A-Za-z\s]+)";
            var cedulaPattern = @"\b\d{1}\s\d{4}\s\d{4}\b";
            var firstLastName = @"1[%][\s]*[Aa]pellido[:\s]*([\w]+)";
            var secondLastName = @"2\d+[\s\W]*[Aa]pellido[:\s]*([A-Za-z]+)(?=\W|$)";

            var cedulaMatch = Regex.Match(text, cedulaPattern);
            string cedula= cedulaMatch.ToString() ;

            var firstLastNameMatch = Regex.Match(text, firstLastName, RegexOptions.IgnoreCase);
            var lastname = firstLastNameMatch.Success ? CleanText(firstLastNameMatch.Groups[1].Value.Trim()) : null;
            string lastname1 = lastname.ToString() ;
            

            var secondLastNameMatch = Regex.Match(text, secondLastName, RegexOptions.IgnoreCase);
            var secondlast = secondLastNameMatch.Success ? CleanText(secondLastNameMatch.Groups[1].Value.Trim()) : null;
            string lastname2 = secondlast.ToString() ;


            var namematch = Regex.Match(text, namePattern, RegexOptions.IgnoreCase);
            var Name = namematch.Success ? CleanText( namematch.Groups[1].Value.Trim()) : null;
            string name = Name.ToString();

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
            // Eliminar caracteres especiales y saltos de línea innecesarios
            // Reemplazar saltos de línea y múltiples espacios por uno solo
            text = Regex.Replace(text, @"[\r\n]+", " ");
            text = Regex.Replace(text, @"\s+", " ");

            // Eliminar secuencias que contienen números seguidos de un carácter especial o letra después de "/"
            text = Regex.Replace(text, @"/\d+\w", "");
            text = Regex.Replace(text, @"/\w+", "");

            // Limpiar patrones no deseados antes de los apellidos
            text = Regex.Replace(text, @"\d[\s°%]*[Aa]pellido", "Apellido");
            text = text.Trim();
            return text;
        }
        
    }
}
