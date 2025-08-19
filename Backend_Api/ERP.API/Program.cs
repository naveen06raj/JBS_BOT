using ERP.API.Services;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register IDbConnection for Dapper
builder.Services.AddScoped<System.Data.IDbConnection>(sp => new Npgsql.NpgsqlConnection(connectionString));

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP API",
        Version = "v1",
        Description = "API for the ERP system"
    });

    // Enable XML comments in Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Include API Explorer settings
    c.DocInclusionPredicate((docName, apiDesc) => apiDesc.ActionDescriptor.EndpointMetadata.All(x => x is not ApiExplorerSettingsAttribute ax || !ax.IgnoreApi));

    // Add JWT Authentication support in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    // Group endpoints by controller
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });    // Generate operation IDs based on controller and action name
    c.CustomOperationIds(apiDesc =>
    {
        var controller = apiDesc.ActionDescriptor.RouteValues["controller"];
        var action = apiDesc.ActionDescriptor.RouteValues["action"];
        return $"{controller}_{action}";
    });

    // Enable annotations
    c.EnableAnnotations();
});

// Add services to the container.
builder.Services.AddScoped<ISalesOpportunityService>(sp =>
    new SalesOpportunityService(connectionString, sp.GetRequiredService<ILogger<SalesOpportunityService>>()));
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<ISalesOrderGridService, SalesOrderGridService>();
builder.Services.AddScoped<ISalesQuotationGridService, SalesQuotationGridService>();
builder.Services.AddScoped<ISalesDemoGridService, SalesDemoGridService>();
builder.Services.AddScoped<ISalesDemoService, SalesDemoService>();
builder.Services.AddScoped<ISalesDealService, SalesDealService>();
builder.Services.AddScoped<ISalesDealGridService, SalesDealGridService>();  // Add this line

builder.Services.AddScoped<SalesLeadService>(sp =>
    new SalesLeadService(connectionString, sp.GetRequiredService<ILogger<SalesLeadService>>()));
builder.Services.AddScoped<SalesQuotationService>(sp =>
    new SalesQuotationService(connectionString));
builder.Services.AddScoped<QuotationService>(sp =>
{
    var leadService = sp.GetRequiredService<SalesLeadService>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var config = sp.GetRequiredService<IConfiguration>();
    return new QuotationService(leadService, env, config);
});

builder.Services.AddControllers(options =>
{
    options.RespectBrowserAcceptHeader = true;
    options.ReturnHttpNotAcceptable = true;
    options.FormatterMappings.SetMediaTypeMappingForFormat("json", "application/json");
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.WriteIndented = true;
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
})
.AddMvcOptions(options =>
{
    options.RespectBrowserAcceptHeader = true;
    options.ReturnHttpNotAcceptable = true;
    options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.StringOutputFormatter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressConsumesConstraintForFormFileParameters = true;
    options.SuppressInferBindingSourcesForParameters = true;
    options.InvalidModelStateResponseFactory = context =>
    {
        var result = new BadRequestObjectResult(new
        {
            message = "Invalid model state",
            statusCode = 400,
            errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
        });
        result.ContentTypes.Add("application/json");
        return result;
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
               .AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Register existing services
builder.Services.AddScoped<SalesQuotationService>(sp =>
    new SalesQuotationService(connectionString));
builder.Services.AddScoped<SalesLeadService>(sp =>
    new SalesLeadService(connectionString, sp.GetRequiredService<ILogger<SalesLeadService>>()));
builder.Services.AddScoped<SalesContactService>(sp =>
    new SalesContactService(connectionString));
builder.Services.AddScoped<SalesAddressService>(sp =>
    new SalesAddressService(connectionString));
builder.Services.AddScoped<SalesLeadsBusinessChallengeService>(sp =>
    new SalesLeadsBusinessChallengeService(connectionString));
builder.Services.AddScoped<SalesDemoInventoryService>(sp =>
    new SalesDemoInventoryService(builder.Configuration));

// Register Bank Account service
builder.Services.AddScoped<ISalesBankAccountService, SalesBankAccountService>();
// Register Terms and Conditions service
builder.Services.AddScoped<ISalesTermsAndConditionsService, SalesTermsAndConditionsService>();

builder.Services.AddScoped<SalesActivityMeetingService>(sp =>
    new SalesActivityMeetingService(connectionString));
builder.Services.AddScoped<SalesProductsService>(sp =>
    new SalesProductsService(connectionString));
builder.Services.AddScoped<SalesActivityEventService>(sp =>
    new SalesActivityEventService(connectionString));
builder.Services.AddScoped<SalesActivityCallService>(sp =>
    new SalesActivityCallService(connectionString));
builder.Services.AddScoped<SalesActivityTaskService>(sp =>
    new SalesActivityTaskService(connectionString));
builder.Services.AddScoped<InventoryItemService>(sp =>
    new InventoryItemService(connectionString, sp.GetRequiredService<IHttpContextAccessor>()));

// Add SalesExternalCommentService
builder.Services.AddScoped<SalesExternalCommentService>(sp =>
    new SalesExternalCommentService(connectionString));

// Add SalesSummaryService
builder.Services.AddScoped<SalesSummaryService>(sp =>
    new SalesSummaryService(connectionString));

// Register location services
builder.Services.AddScoped<SalesCountryService>(sp =>
    new SalesCountryService(connectionString));
builder.Services.AddScoped<SalesStateService>(sp =>
    new SalesStateService(connectionString));
builder.Services.AddScoped<SalesTerritoryService>(sp =>
    new SalesTerritoryService(connectionString));
builder.Services.AddScoped<SalesDistrictService>(sp =>
    new SalesDistrictService(connectionString));
builder.Services.AddScoped<SalesCityService>(sp =>
    new SalesCityService(connectionString));
builder.Services.AddScoped<SalesAreaService>(sp =>
    new SalesAreaService(connectionString));
builder.Services.AddScoped<SalesPincodeService>(sp =>
    new SalesPincodeService(connectionString));

builder.Services.AddScoped<SalesLocationService>(sp =>
    new SalesLocationService(connectionString));
// Add SalesDocumentService
builder.Services.AddScoped<SalesDocumentService>();

// Add InternalDiscussionService
builder.Services.AddScoped<InternalDiscussionService>(sp =>
    new InternalDiscussionService(connectionString)); 
// Add GeographicalDivisionService
builder.Services.AddScoped<IGeographicalDivisionService, GeographicalDivisionService>();
// Register IUserService and UserService
builder.Services.AddScoped<IUserService, UserService>();
// Add SalesDemoAssignmentService
builder.Services.AddScoped<SalesDemoAssignmentService>(sp =>
    new SalesDemoAssignmentService(builder.Configuration));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP API v1");
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.EnableFilter();
        c.DisplayRequestDuration();
    });
}

// Use CORS before other middleware
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Configure endpoints
app.MapControllers().RequireCors("AllowAll");

app.Run();
