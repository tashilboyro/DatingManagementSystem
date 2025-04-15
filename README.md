# Dating Management System


 

LoveBirds is a dating management system designed to help users discover their most compatible matches.

New users can register and log in to the platform. Upon registration, a compatibility score is automatically calculated between the new user and all existing users in the LoveBirdsDatabase using an intelligent matching algorithm.

When a user logs in, their top matches are displayed on the user interface. Users can remove a suggested match at any time by clicking the Skip button on the profile card.

 

1. Registered users are stored in the `Users` table in the `LoveBirdsDatabase` database.


2. Logged in users are stored in the session storage.


3. Compatibility scores between 2 users are stored in the `CompatibilityScores` table in the `LoveBirdsDatabase`

   database.


4. Removed Users i.e skipped users are stored in ``SkippedUsers`` table in th database.

<br>

 

## Migration/ Database

## Step 1: Installing Packages

1] Install-Package Microsoft.EntityFrameworkCore.SqlServer <br>

2] Install-Package Microsoft.EntityFrameworkCore.Tools

 

### Run this Command

`dotnet ef migrations list` <br>

There are 3 migrations that need to be completed for the database to operate. The 2 migrations are: <br>

1. InitialCreate

2. AddCompatibilityScoresTable

3. AddSkippedUsersTable

 

When running the above 'dotnet ef migrations list command', if any of the above 2 migrations are pending

run these commands on the NuGet package manager console to make sure the migrations are completed:

Run `dotnet ef database update`

If InitialCreate is pending run `dotnet ef database update 20250303172637_InitialCreate` <br>

If AddCompatibilityScoresTable is pending run `dotnet ef database update 20250306062912_AddCompatibilityScores`

If AddCompatibilityScoresTable is pending run `dotnet ef database updte 20250413124504_AddSkippedUsersTable`

 

If you are having any issues with the migration, simply create the tables manually on the LoveBirdsDatabase in your

SQL server management studio (SSMS).

 

### Creating Users Table Manually

 

```
CREATE TABLE Users (

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

 

```
CREATE TABLE CompatibilityScores (

    Id INT IDENTITY(1,1) PRIMARY KEY,

    User1Id INT NOT NULL,

    User2Id INT NOT NULL,

    Score FLOAT NOT NULL,

    CONSTRAINT FK_CompatibilityScore_User1 FOREIGN KEY (User1Id) REFERENCES Users(UserID),

    CONSTRAINT FK_CompatibilityScore_User2 FOREIGN KEY (User2Id) REFERENCES Users(UserID),

    CONSTRAINT UQ_CompatibilityScore UNIQUE (User1Id, User2Id)

);

```

### Creating Users Table Manually

```
CREATE TABLE SkippedUsers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    SkippedUserId INT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserID),
    FOREIGN KEY (SkippedUserId) REFERENCES Users(UserID)
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


## Testing
Unit testing for this project was conducted using MSTest. 
All test cases are organized within the test project file `DatingManagementSystem.Tests.csproj`, which is included in the Git repository.