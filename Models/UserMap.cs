using CsvHelper.Configuration;
using DatingManagementSystem.Models;

public class UserMap : ClassMap<User>
{
    public UserMap()
    {
        //Map(m => m.UserID);
        Map(m => m.FirstName);
        Map(m => m.LastName);
        Map(m => m.Age);
        Map(m => m.Gender);
        Map(m => m.Email);
        Map(m => m.Password);
        Map(m => m.Interests);
        Map(m => m.Bio);
        Map(m => m.CreatedAt).TypeConverterOption.Format("dd/MM/yyyy HH:mm");
        Map(m => m.ProfilePicture).Convert(row =>
        {
            var base64String = row.Row.GetField("ProfilePicture");
            return string.IsNullOrWhiteSpace(base64String) ? null : Convert.FromBase64String(base64String);
        });
    }
}