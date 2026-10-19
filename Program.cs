using BlazorApp2.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace BlazorApp2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                });
            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapGet("/logout", async (HttpContext ctx) =>
            {
                await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.LocalRedirect("/");
            });
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=bookcatalog.db"))
            {
                connection.Open();

                var createBooks = connection.CreateCommand();
                createBooks.CommandText = @"
        CREATE TABLE IF NOT EXISTS Books (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL,
            Author TEXT NOT NULL,
            Total INTEGER NOT NULL,
            Available INTEGER NOT NULL
        )";
                createBooks.ExecuteNonQuery();

                var createUsers = connection.CreateCommand();
                createUsers.CommandText = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            PasswordHash TEXT NOT NULL,
            Salt TEXT NOT NULL,
            Role TEXT NOT NULL
        )";
                createUsers.ExecuteNonQuery();

                var countCommand = connection.CreateCommand();
                countCommand.CommandText = "SELECT COUNT(*) FROM Users";
                long userCount = (long)countCommand.ExecuteScalar()!;

                if (userCount == 0)
                {
                    // Elev
                    var salt1 = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
                    using var pbkdf2_1 = new System.Security.Cryptography.Rfc2898DeriveBytes("1234", Convert.FromBase64String(salt1), 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
                    string hash1 = Convert.ToBase64String(pbkdf2_1.GetBytes(32));

                    var seedCommand1 = connection.CreateCommand();
                    seedCommand1.CommandText = @"
        INSERT INTO Users (Name, PasswordHash, Salt, Role)
        VALUES ('elev1', $hash, $salt, 'Elev')";
                    seedCommand1.Parameters.AddWithValue("$hash", hash1);
                    seedCommand1.Parameters.AddWithValue("$salt", salt1);
                    seedCommand1.ExecuteNonQuery();

                    // Bibliotekar
                    var salt2 = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
                    using var pbkdf2_2 = new System.Security.Cryptography.Rfc2898DeriveBytes("admin123", Convert.FromBase64String(salt2), 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
                    string hash2 = Convert.ToBase64String(pbkdf2_2.GetBytes(32));

                    var seedCommand2 = connection.CreateCommand();
                    seedCommand2.CommandText = @"
        INSERT INTO Users (Name, PasswordHash, Salt, Role)
        VALUES ('biblo1', $hash, $salt, 'Bibliotekar')";
                    seedCommand2.Parameters.AddWithValue("$hash", hash2);
                    seedCommand2.Parameters.AddWithValue("$salt", salt2);
                    seedCommand2.ExecuteNonQuery();
                }
            }

            app.Run();
        }
    }
}
