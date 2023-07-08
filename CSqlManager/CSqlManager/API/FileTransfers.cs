﻿
namespace CSqlManager;

public class FileTransfers: SecureEnpoint
{
    public static void MapEndPoints(WebApplication app)
    {
        app.MapGet("/files/{id}", GetFile);
        app.MapPost("/files/{id}", PostFile);
    }
    
    // To change depending on the context
    private static readonly string UploadDirectory = "C:/temp" ;

    // Needs an update 
    static async Task<Task> GetFile(HttpContext context, int id)
    {
        JwtClaims claims = getJwtClaims(context);
        if (!claims.Valid) {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            Console.WriteLine("ERROR 401 : Invalid JWT");
            return Task.CompletedTask;
        }
        var access = new TicketAccess();
        var ticket = access.GetById(id);
        if ((ticket == null) || (ticket.tenant == null) || 
            (ticket.file_id == null) || (ticket.file_name == null)  || ((claims.Tenant != ticket.tenant) && (claims.Profile != "ADMIN" && claims.Profile != "OPERATOR"))) {
     
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            Console.WriteLine("ERROR 401 : Invalid JWT");
            return Task.CompletedTask;
        }

        // get the file
        String directory = FileTransfers.BuildDirectory(ticket.tenant!, ticket.file_id);
        var filePath = Path.Combine(directory, ticket.file_name);

        //context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{ticket.file_name}\"";
        context.Response.ContentType = "application/octet-stream";
        FileInfo fileInfo = new FileInfo(filePath);
        context.Response.ContentLength = fileInfo.Length;
        if (!File.Exists(filePath))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return Task.CompletedTask;
        }
        
        using (var fileStream = File.OpenRead(filePath))
        {
            await fileStream.CopyToAsync(context.Response.Body);
            //fileStream.CopyTo(context.Response.Body);
        }
        
        return Task.CompletedTask;
    }

    static Task PostFile(HttpContext context, int id)
    {
        JwtClaims claims = getJwtClaims(context);
        if (!claims.Valid) {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            Console.WriteLine("ERROR 401 : Invalid JWT");
            return Task.CompletedTask;
        }
        Console.WriteLine("Post treatment of a file :");
        var file = context.Request.Form.Files.FirstOrDefault();

        if (file == null || file.Length == 0)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            Console.WriteLine("ERROR 400 : No file was found");
            return Task.CompletedTask;
        }
        // increase counter
        var tenant = claims.Tenant;
        var access = new TenantAccess();
        access.nextFileId(tenant);
        var fileName = "file.bin";
 
        foreach (var file1 in context.Request.Form.Files)
        {
            if (file1.FileName != null)
            {
                fileName = file1.FileName;
            }
        }
/*        foreach (var formPart in context.Request.Form) {
            if (formPart.Key == "filename") {
               fileName = formPart.Value;
            }
            else if (formPart.Key == "ticket_id") {
               ticket_id = formPart.Value;
            }
            else if (formPart.Key == "tenant") {
               tenant = formPart.Value;
            }
            else
            {
                Console.WriteLine("WARNING UNKNOWN PARAMETER :" + formPart.Key + " with value :" + formPart.Value);
            }
        }*/
        Console.WriteLine("file informations : " + fileName+" "+id+" "+tenant+" CLAIMS: "+claims);
        
        string fileLocation = BuildDirectory(tenant, id);
        var filePath = Path.Combine(fileLocation, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(fileStream);
        }

        context.Response.StatusCode = StatusCodes.Status201Created;
        context.Response.Headers["Location"] = fileLocation;
        Console.WriteLine("File Saved in : " + fileLocation);
        Console.WriteLine();
        return Task.CompletedTask;
    }

    public static string BuildDirectory(string tenant, int id)
    {
        string fileId = BuildFileId(tenant, id);
        string Dlocation = BuildDirectory(tenant, fileId);
        CreateDirectoryTree(Dlocation);
        Console.WriteLine("Writing to : "+Dlocation);
        return Dlocation;
    }

      public static string BuildDirectory(string tenant, string fileId)
    {
        string Dlocation = $"{UploadDirectory}/{fileId}";
        return Dlocation;
    }

     public static string BuildFileId(string tenant, int id)
    {
        DateTime currentDate = DateTime.Now;
        int monthNumber = currentDate.Month;
        string monthFolder = monthNumber < 10 ? "0"+monthNumber : monthNumber.ToString();
        string key = $"{monthFolder}/{id}";

        return key;
    }

    public static void CreateDirectoryTree(string path)
    {
        if (!Directory.Exists(path))
        {
            // Create the directory
            Directory.CreateDirectory(path);
        }
        else
        {
            // Directory already exists, exit recursion
            return;
        }

        // Get the parent directory
        string parentDirectory = Path.GetDirectoryName(path);

        // Recursively create parent directories
        if (!string.IsNullOrEmpty(parentDirectory))
        {
            CreateDirectoryTree(parentDirectory);
        }
    }
}