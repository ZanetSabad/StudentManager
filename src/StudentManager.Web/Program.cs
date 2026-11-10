using Microsoft.EntityFrameworkCore;
using StudentManager.Web.Data;
using StudentManager.Web.Options;
using StudentManager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();


// Databáze: SQLite, adresa se bere z appsettings.json (ConnectionStrings:DefaultConnection)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Nastavení školy (doména…) ze sekce "School" v appsettings.json.
// Stránka si ho vyžádá jako IOptions<SchoolOptions>.
builder.Services.Configure<SchoolOptions>(builder.Configuration.GetSection(SchoolOptions.SectionName));

// PROČ Singleton: služby si nic nepamatují, stačí jedna instance pro celou aplikaci.
builder.Services.AddSingleton<UsernameGenerator>();
builder.Services.AddSingleton<UsernameValidator>();
builder.Services.AddSingleton<StudentStatusEvaluator>();

var app = builder.Build();


// Testovací data – JEN při vývoji na vlastním PC.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(context);
}



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
