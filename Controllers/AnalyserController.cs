using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace SYNCit.Controllers
{
    public class AnalyserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Analyse(IFormFile cvFile, string jobDescription)
        {
            if (cvFile == null || cvFile.Length == 0)
            {
                ViewBag.Error = "Please upload a CV file.";
                return View("Index");
            }

            string cvText = "";
            var extension = Path.GetExtension(cvFile.FileName).ToLower();

            if (extension == ".docx")
            {
                using var stream = cvFile.OpenReadStream();
                using var doc = WordprocessingDocument.Open(stream, false);
                var body = doc.MainDocumentPart!.Document.Body;
                cvText = string.Join(" ", body!.Descendants<Text>().Select(t => t.Text));
            }
            else if (extension == ".txt")
            {
                using var reader = new StreamReader(cvFile.OpenReadStream());
                cvText = await reader.ReadToEndAsync();
            }
            else
            {
                ViewBag.Error = "Only .docx and .txt files are supported.";
                return View("Index");
            }

            if (string.IsNullOrWhiteSpace(cvText))
            {
                ViewBag.Error = "Could not extract text from the file.";
                return View("Index");
            }

            // Save inputs to temp files
            var tempCvPath = Path.GetTempFileName();
            var tempJdPath = Path.GetTempFileName();
            await System.IO.File.WriteAllTextAsync(tempCvPath, cvText);
            await System.IO.File.WriteAllTextAsync(tempJdPath, jobDescription);

            var pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "analyser.py");

            var start = new ProcessStartInfo
            {
               FileName = @"C:\Users\sinet\AppData\Local\Python\bin\python.exe",
                Arguments = $"\"{pythonScriptPath}\" \"{tempCvPath}\" \"{tempJdPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(start);
            string output = process!.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Cleanup temp files
            System.IO.File.Delete(tempCvPath);
            System.IO.File.Delete(tempJdPath);

            if (string.IsNullOrWhiteSpace(output))
            {
                ViewBag.Error = $"Python error: {error}";
                return View("Index");
            }

            ViewBag.Result = output;
            ViewBag.Debug = $"CV Text Length: {cvText.Length} | Output: '{output}' | Script Path: {pythonScriptPath}";
            return View("Results");
        }
    }
}