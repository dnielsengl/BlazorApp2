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
                    var salt = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
                    using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes("1234", Convert.FromBase64String(salt), 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
                    string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));

                    var seedCommand = connection.CreateCommand();
                    seedCommand.CommandText = @"
            INSERT INTO Users (Name, PasswordHash, Salt, Role)
            VALUES ('elev1', $hash, $salt, 'Elev')";
                    seedCommand.Parameters.AddWithValue("$hash", hash);
                    seedCommand.Parameters.AddWithValue("$salt", salt);
                    seedCommand.ExecuteNonQuery();
                }
            }

            app.Run();
        }
    }
}
