using System.IO;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Hubs;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("MangoTaikaIntegrationTests"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = false;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
})
.AddErrorDescriber<FrenchIdentityErrorDescriber>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddTokenProvider<PhoneNumberTokenProvider<ApplicationUser>>("Phone");

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName("MangoTaika");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddHttpClient("Nominatim");
builder.Services.AddHttpClient("OrangeSMS");
builder.Services.AddHttpClient("Twilio");
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IGeocodingService, GeocodingService>();
builder.Services.AddScoped<IScoutService, ScoutService>();
builder.Services.AddScoped<IScoutQrService, ScoutQrService>();
builder.Services.AddScoped<IGroupeService, GroupeService>();
builder.Services.AddScoped<DistrictBranchInheritanceService>();
builder.Services.AddScoped<IActiviteService, ActiviteService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IBrancheService, BrancheService>();
builder.Services.AddScoped<IActualiteService, ActualiteService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IFormationService, FormationService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<INotificationDispatchService, NotificationDispatchService>();
builder.Services.AddScoped<OperationalAccessService>();
builder.Services.AddScoped<IClaimsTransformation, CommissaireDistrictClaimsTransformation>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ActiveRoleService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddSignalR();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "1; mode=block";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https://*.tile.openstreetmap.org; " +
        "connect-src 'self' wss: ws: https://nominatim.openstreetmap.org; " +
        "frame-src 'self' https://www.youtube.com https://player.vimeo.com;";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var districtBranchInheritance = scope.ServiceProvider.GetRequiredService<DistrictBranchInheritanceService>();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.MigrateAsync();
    }

    var formationsSansConfiguration = await db.Formations
        .Where(f => !f.DelivranceConfiguree)
        .ToListAsync();

    if (formationsSansConfiguration.Count != 0)
    {
        foreach (var formation in formationsSansConfiguration)
        {
            formation.DelivreBadge = true;
            formation.DelivreAttestation = true;
            formation.DelivreCertificat = false;
            formation.DelivranceConfiguree = true;
        }

        await db.SaveChangesAsync();
    }

    foreach (var roleName in RoleNames.All)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });
        }
    }

    var adminEmail = builder.Configuration["AdminSeed:Email"] ?? "admin@mangotaika.com";
    var adminPhone = builder.Configuration["AdminSeed:Phone"] ?? "0000000000";
    var adminPassword = builder.Configuration["AdminSeed:Password"];
    const string adminRole = RoleNames.Administrateur;

    if (!string.IsNullOrWhiteSpace(adminPassword) && !await db.Users.AnyAsync(u => u.PhoneNumber == adminPhone))
    {
        var admin = new ApplicationUser
        {
            UserName = adminPhone,
            PhoneNumber = adminPhone,
            Email = adminEmail,
            Nom = "Admin",
            Prenom = "MANGO TAIKA",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, adminRole);
        }
    }

    var seedDemoData = builder.Configuration.GetValue("SeedDemoData", app.Environment.IsDevelopment());
    if (!app.Environment.IsEnvironment("Testing") && seedDemoData)
    {
        await SeedData.InitializeAsync(scope.ServiceProvider);
    }

    await districtBranchInheritance.EnsureInheritedBranchesAsync();

    const string oldFacebookUrl = "https://www.facebook.com/mangotaika";
    const string newFacebookUrl = "https://www.facebook.com/share/1BAtkMx8sd/";
    const string oldInstagramUrl = "https://www.instagram.com/mangotaika";
    const string newInstagramUrl = "https://www.instagram.com/scouts_du_darp?igsh=ajc0eDB5ODNtaWk0";
    const string oldWhatsappUrl = "https://wa.me/2250707070700";
    const string plainWhatsappUrl = "https://wa.me/2250759013291";
    const string newTiktokUrl = "https://www.tiktok.com/@scoutsmangotaika?_r=1&_t=ZS-94zhRDuRtOW";
    const string whatsappSelectorUrl = "/Home/WhatsApp";

    var facebookLink = await db.LiensReseauxSociaux
        .OrderBy(l => l.Ordre)
        .FirstOrDefaultAsync(l => l.Plateforme == "Facebook");

    if (facebookLink is null)
    {
        var nextOrder = await db.LiensReseauxSociaux.AnyAsync()
            ? await db.LiensReseauxSociaux.MaxAsync(l => l.Ordre) + 1
            : 1;

        db.LiensReseauxSociaux.Add(new LienReseauSocial
        {
            Id = Guid.NewGuid(),
            Plateforme = "Facebook",
            Url = newFacebookUrl,
            Icone = "bi-facebook",
            EstActif = true,
            Ordre = nextOrder
        });

        await db.SaveChangesAsync();
    }
    else if (string.Equals(facebookLink.Url, oldFacebookUrl, StringComparison.OrdinalIgnoreCase))
    {
        facebookLink.Url = newFacebookUrl;
        facebookLink.Icone ??= "bi-facebook";
        facebookLink.EstActif = true;
        await db.SaveChangesAsync();
    }

    var instagramLink = await db.LiensReseauxSociaux
        .OrderBy(l => l.Ordre)
        .FirstOrDefaultAsync(l => l.Plateforme == "Instagram");

    if (instagramLink is null)
    {
        var nextOrder = await db.LiensReseauxSociaux.AnyAsync()
            ? await db.LiensReseauxSociaux.MaxAsync(l => l.Ordre) + 1
            : 1;

        db.LiensReseauxSociaux.Add(new LienReseauSocial
        {
            Id = Guid.NewGuid(),
            Plateforme = "Instagram",
            Url = newInstagramUrl,
            Icone = "bi-instagram",
            EstActif = true,
            Ordre = nextOrder
        });

        await db.SaveChangesAsync();
    }
    else if (string.Equals(instagramLink.Url, oldInstagramUrl, StringComparison.OrdinalIgnoreCase))
    {
        instagramLink.Url = newInstagramUrl;
        instagramLink.Icone ??= "bi-instagram";
        instagramLink.EstActif = true;
        await db.SaveChangesAsync();
    }

    var whatsappLink = await db.LiensReseauxSociaux
        .OrderBy(l => l.Ordre)
        .FirstOrDefaultAsync(l => l.Plateforme == "WhatsApp");

    if (whatsappLink is null)
    {
        var nextOrder = await db.LiensReseauxSociaux.AnyAsync()
            ? await db.LiensReseauxSociaux.MaxAsync(l => l.Ordre) + 1
            : 1;

        db.LiensReseauxSociaux.Add(new LienReseauSocial
        {
            Id = Guid.NewGuid(),
            Plateforme = "WhatsApp",
            Url = whatsappSelectorUrl,
            Icone = "bi-whatsapp",
            EstActif = true,
            Ordre = nextOrder
        });

        await db.SaveChangesAsync();
    }
    else if (string.Equals(whatsappLink.Url, oldWhatsappUrl, StringComparison.OrdinalIgnoreCase)
        || string.Equals(whatsappLink.Url, plainWhatsappUrl, StringComparison.OrdinalIgnoreCase)
        || whatsappLink.Url.StartsWith(plainWhatsappUrl, StringComparison.OrdinalIgnoreCase)
        || string.Equals(whatsappLink.Url, whatsappSelectorUrl, StringComparison.OrdinalIgnoreCase))
    {
        whatsappLink.Url = whatsappSelectorUrl;
        whatsappLink.Icone ??= "bi-whatsapp";
        whatsappLink.EstActif = true;
        await db.SaveChangesAsync();
    }

    var youtubeLinks = await db.LiensReseauxSociaux
        .Where(l => l.Plateforme == "YouTube" && l.EstActif)
        .ToListAsync();

    if (youtubeLinks.Count != 0)
    {
        foreach (var youtubeLink in youtubeLinks)
        {
            youtubeLink.EstActif = false;
        }

        await db.SaveChangesAsync();
    }

    var tiktokLink = await db.LiensReseauxSociaux
        .OrderBy(l => l.Ordre)
        .FirstOrDefaultAsync(l => l.Plateforme == "TikTok");

    if (tiktokLink is null)
    {
        var nextOrder = await db.LiensReseauxSociaux.AnyAsync()
            ? await db.LiensReseauxSociaux.MaxAsync(l => l.Ordre) + 1
            : 1;

        db.LiensReseauxSociaux.Add(new LienReseauSocial
        {
            Id = Guid.NewGuid(),
            Plateforme = "TikTok",
            Url = newTiktokUrl,
            Icone = "bi-tiktok",
            EstActif = true,
            Ordre = nextOrder
        });

        await db.SaveChangesAsync();
    }
}

app.Run();

public partial class Program { }
