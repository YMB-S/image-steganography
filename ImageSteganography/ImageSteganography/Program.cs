using ImageSteganography.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//var appSettings = builder.Configuration.GetValue<AppSettings>("AppSettings");
//if(appSettings != null)
//{
//    builder.Services.AddSingleton(appSettings);
//}
//else
//{
//    throw new InvalidOperationException();
//}

//var allowedFileTypes = builder.Configuration.GetValue<string>("AppSettings:AllowedFileTypes");



//builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AllowedFileTypes"));

builder.Services.AddSingleton<ImageSteganographyService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
