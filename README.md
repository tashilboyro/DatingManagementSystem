# Dating Management System

 

LoveBirds is a dating management system where users can find their perfect match.

Users may register and then log in. Upon registration, the compatibility score of that user is calculated

against all the existing users in the database (LoveBirdsDatabase). Then upon login,

the most compatible users with that logged in user is displayed on the user interface.

 

1. Registered users are stored in the `Users` table in the `LoveBirdsDatabase` database.

<br>

2. Logged in users are stores in the session storage.

<br>

3. Compatibility scores between 2 users are stored in the `CompatibilityScores` table in the `LoveBirdsDatabase`

database.

<br>

 

## Migration/ Database

## Step 1: Installing Packages

1] Install-Package Microsoft.EntityFrameworkCore.SqlServer <br>

2] Install-Package Microsoft.EntityFrameworkCore.Tools

 

### Run this Command

`dotnet ef migrations list` <br>

There are 2 migrations that need to be completed for the database to operate. The 2 migrations are: <br>

1. InitialCreate

2. AddCompatibilityScoresTable

 

When running the above 'dotnet ef migrations list command', if any of the above 2 migrations are pending

run these commands on the NuGet package manager console to make sure the migrations are completed:

 

If InitialCreate is pending run `dotnet ef migrations add InitialCreate` <br>

If AddCompatibilityScoresTable is pending run `dotnet ef migrations add AddCompatibilityScoresTable`

 

If you are having any issues with the migration, create the tables manually on the LoveBirdsDatabase in your

SQL server management studio (SSMS).

 

### Creating Users Table Manually

 

```CREATE TABLE Users (

    UserID INT PRIMARY KEY IDENTITY(1,1),

    FirstName NVARCHAR(100) NOT NULL,

    LastName NVARCHAR(100) NOT NULL,

    Age INT NOT NULL,

    Gender NVARCHAR(10) NOT NULL CHECK (Gender IN ('Male', 'Female')),

    Email NVARCHAR(255) UNIQUE NOT NULL,

    Password NVARCHAR(255) NOT NULL, -- Hashed password storage

    Interests NVARCHAR(MAX) NOT NULL, -- Comma-separated values

    ProfilePicture VARBINARY(MAX), -- Storing binary image data

    Bio NVARCHAR(500),

    CreatedAt DATETIME DEFAULT GETDATE()

);

```

 

### Creating CompatibilityScores Table Manually

 

```CREATE TABLE CompatibilityScores (

    Id INT IDENTITY(1,1) PRIMARY KEY,

    User1Id INT NOT NULL,

    User2Id INT NOT NULL,

    Score FLOAT NOT NULL,

    CONSTRAINT FK_CompatibilityScore_User1 FOREIGN KEY (User1Id) REFERENCES Users(UserID),

    CONSTRAINT FK_CompatibilityScore_User2 FOREIGN KEY (User2Id) REFERENCES Users(UserID),

    CONSTRAINT UQ_CompatibilityScore UNIQUE (User1Id, User2Id)

);

```

## Step 2: Connection String

Modify your connection string in appsettings.json. <br>

``` "AllowedHosts": "*",

    "ConnectionStrings": {

        "DefaultConnection": "Server=YOUR_USERNAME;Database=LoveBirdsDatabase;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"

    }

```

 

## Step 3: CSV Helper Package

Open your NuGet package manager console and run this command: `Install-Package CsvHelper`

 

## Step 4: Run Project

Run the LoveBirds Project.

 

## Step 5: Upload CSV File

Click on `Sign up` and upload the provided CSV file in the BULK upload CSV File section on the user interface.<br>

 

This code snippet from Userscontroller explains what happens when the CSV File is uploaded. First all the users enter the database then for each of them,

the compatibility score against all the other users are calculated and stored in respective tables in the LoveBirdsDatabase.

 

```

   public async Task<IActionResult> UploadUsers(IFormFile csvFile)

        {

            if (csvFile == null || csvFile.Length == 0)

            {

                ModelState.AddModelError("", "CSV file is required.");

                return View("Index");

            }

 

            var users = new List<User>();

 

            using var reader = new StreamReader(csvFile.OpenReadStream());

            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)

            {

                HasHeaderRecord = true,  // Assumes the CSV file has a header

                Delimiter = ",", // Assumes CSV file uses comma as delimiter

                Quote = '"', // Handle quoted fields like Interests that contain commas

            });

 

            try

            {

                csv.Context.RegisterClassMap<UserMap>();

           

                // Read records and parse the rows into User objects

                var records = csv.GetRecords<User>();

 

                foreach (var record in records)

                {

                    users.Add(record);

                }

            }

            catch (CsvHelperException ex)

            {

                _logger.LogWarning($"CSV parsing failed: {ex.Message}");

                return View("Create");

            }

 

            // Add valid users to the database

            _context.Users.AddRange(users);

            await _context.SaveChangesAsync();

 

            // Compute compatibility for all users

            foreach (var user in users)

            {

                ComputeCompatibility(user);

            }

 

            return RedirectToAction(nameof(Login));

        }

```

 

Once the CSV file is uploaded, let the tasks keep running on your command prompt, and you may minimize the

commandprompt. Since there are 1000 rows of data in total, the compatibility score for each user will take some time

to finish fully.

 

## Step 6: Log in

Click on `Back to Login` and then log in using any user and his/her password from the Users table in the database. <br>

 

Refer to video to see how the most compatible users are displayed on the user interface.