using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using WritingIdentifier.Models;

namespace WritingIdentifier.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetOutput()
        {
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "credentials.json");
            GoogleCredential credential;
            using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(new[] { DriveService.Scope.Drive });
            }

            // Create a new DriveService using the GoogleCredential
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "WritingIdentifier"
            });

            // Use the DriveService to access the user's data in Google Drive

            // Use the DriveService to access the files in the folder
            string folderId = "1rtg_q3nnZ25aUZTl4_mzSH5UZxhi-KSU";
            var request = service.Files.List();
            request.Q = $" '{folderId}' in parents"; // Search for files in the specified folder
            request.Fields = "nextPageToken, files(id, name, mimeType)"; // Define the fields you want to retrieve
            request.IncludeItemsFromAllDrives = true;
            request.SupportsAllDrives = true;
            request.PageSize = 1000;
            var result = request.Execute();
            var files = result.Files;

            var stringDta = JsonSerializer.Serialize(files);

            string outputFileID = string.Empty;
            List<DataModel> receivedFiles = JsonSerializer.Deserialize<List<DataModel>>(stringDta);
            foreach (var file in receivedFiles)
            {
                if (file.Name == "output.txt")
                {
                    outputFileID = file.Id;
                    break;
                }
            }

            string link = $"https://drive.google.com/uc?export=download&id={outputFileID}";
            var httpClient = new HttpClient();

            // send an HTTP GET request to the download link and get the response
            var response = await httpClient.GetAsync(link);

            // read the contents of the file from the response body
            var fileContents = await response.Content.ReadAsStringAsync();

            return Content(fileContents);
        }



        [HttpPost]
        public async Task<IActionResult> TestWriters(List<IFormFile> file, string folderName, string imageName)
        {
            if (file.Count > 1)
            {
                ViewBag.UploadStatus = "Please Select Only One File";
                return View("Index");
            }
            if (file == null || folderName == null || imageName == null)
            {
                return BadRequest("Invalid Request");
            }
            if (Path.GetExtension(file[0].FileName) != ".png")
            {
                ViewBag.UploadStatus = "Invalid File Type";
                return View("Index");
            }



            // Read the credentials.json file from wwwroot folder

            GoogleCredential credential = GoogleCredential.FromFile(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "credentials.json"))
                .CreateScoped(DriveService.ScopeConstants.Drive);

            // Create the Drive API client
            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "WritingIdentifier"
            });

            // Find the parent folder by ID
            string folderId = "1hh9o5YVUD0-VnVx7HfzZFekAQj4UPYP0";

            // Check if the folder already exists
            var listRequest = service.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false and '{folderId}' in parents";
            listRequest.Fields = "files(id)";
            var existingFolder = await listRequest.ExecuteAsync();

            // If the folder already exists
            if (existingFolder.Files.Count > 0)
            {
                var folder = existingFolder.Files.FirstOrDefault();
                // Upload the file to the created folder
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = $"{imageName}.png",
                    Parents = new List<string> { folder.Id },
                };
                using (var stream = file[0].OpenReadStream())
                {
                    var uploadRequest = service.Files.Create(fileMetadata, stream, "image/png");
                    uploadRequest.Fields = "id";
                    await uploadRequest.UploadAsync();
                }
                ViewBag.UploadStatus = "File Uploaded Successfully";
                return View("Index");
            }
            else
            {
                // Create a folder in the parent folder
                var folderMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = folderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = new List<string> { folderId },
                };
                var folderRequest = service.Files.Create(folderMetadata);
                folderRequest.Fields = "id";
                var folder = await folderRequest.ExecuteAsync();

                // Upload the file to the created folder
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = $"{imageName}.png",
                    Parents = new List<string> { folder.Id },
                };
                using (var stream = file[0].OpenReadStream())
                {
                    var uploadRequest = service.Files.Create(fileMetadata, stream, "image/png");
                    uploadRequest.Fields = "id";
                    await uploadRequest.UploadAsync();
                }
                ViewBag.UploadStatus = "File Uploaded Successfully";
                return View("Index");
            }
        }

        [HttpGet]
        public IActionResult Writers()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Writers(List<IFormFile> file, string folderName, string imageName)
        {
            if (file == null || folderName == null || imageName == null)
            {
                return BadRequest("Invalid Request");
            }
            foreach (var uploadedFile in file)
            {
                if (Path.GetExtension(uploadedFile.FileName) != ".png")
                {
                    ViewBag.UploadStatus = "Invalid File Type";
                    return View();
                }
            }
            



            // Read the credentials.json file from wwwroot folder

            GoogleCredential credential = GoogleCredential.FromFile(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "credentials.json"))
                .CreateScoped(DriveService.ScopeConstants.Drive);

            // Create the Drive API client
            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "WritingIdentifier"
            });

            // Find the parent folder by ID
            string folderId = "1pNTHUpWlEJEn3myBn6T0jJDcSehhN65I";

            // Check if the folder already exists
            var listRequest = service.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false and '{folderId}' in parents";
            listRequest.Fields = "files(id)";
            var existingFolder = await listRequest.ExecuteAsync();

            // If the folder already exists
            if (existingFolder.Files.Count > 0)
            {
                var folder = existingFolder.Files.FirstOrDefault();
                // Upload the file to the created folder
                int count = 1;
                foreach (var uploadedFile in file)
                {
                    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = $"{imageName}_{count}.png",
                        Parents = new List<string> { folder.Id },
                    };
                    using (var stream = uploadedFile.OpenReadStream())
                    {
                        var uploadRequest = service.Files.Create(fileMetadata, stream, "image/png");
                        uploadRequest.Fields = "id";
                        await uploadRequest.UploadAsync();
                    }
                    count++;
                }

                ViewBag.UploadStatus = "All Files Uploaded Successfully";
                return View();
            }
            else
            {
                // Create a folder in the parent folder
                var folderMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = folderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = new List<string> { folderId },
                };
                var folderRequest = service.Files.Create(folderMetadata);
                folderRequest.Fields = "id";
                var folder = await folderRequest.ExecuteAsync();

                // Upload the file to the created folder
                int count = 1;
                foreach (var uploadedFile in file)
                {
                    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = $"{imageName}_{count}.png",
                        Parents = new List<string> { folder.Id },
                    };
                    using (var stream = uploadedFile.OpenReadStream())
                    {
                        var uploadRequest = service.Files.Create(fileMetadata, stream, "image/png");
                        uploadRequest.Fields = "id";
                        await uploadRequest.UploadAsync();
                    }
                }
                count++;

                ViewBag.UploadStatus = "All Files Uploaded Successfully";
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}