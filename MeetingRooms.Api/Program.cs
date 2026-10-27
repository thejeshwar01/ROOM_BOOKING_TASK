using System.Text.Json.Serialization;
using MeetingRooms.Api.Data;
using MeetingRooms.Api.Errors;
using MeetingRooms.Api.Options;
using MeetingRooms.Api.Services;
using MeetingRooms.Api.Services.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register the in-memory database
builder.Services.AddDbContext<MeetingRoomContext>(options =>
    options.UseInMemoryDatabase("MeetingRoomsDatabase"));

// Configurable limits (appsettings.json), checked when the app starts
builder.Services.AddOptions<BookingRulesOptions>()
    .Bind(builder.Configuration.GetSection(BookingRulesOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<UsageReportOptions>()
    .Bind(builder.Configuration.GetSection(UsageReportOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Injected clock so time-based rules can be tested
builder.Services.AddSingleton(TimeProvider.System);

// Booking rules run in the order they are registered here
builder.Services.AddScoped<IBookingRule, ValidTimeRangeRule>();
builder.Services.AddScoped<IBookingRule, StartInFutureRule>();
builder.Services.AddScoped<IBookingRule, BookingHorizonRule>();
builder.Services.AddScoped<IBookingRule, MaxDurationRule>();
builder.Services.AddScoped<IBookingRule, ActiveRoomRule>();
builder.Services.AddScoped<IBookingRule, CapacityRule>();
builder.Services.AddScoped<IBookingRule, RoomOverlapRule>();
builder.Services.AddScoped<IBookingRule, OrganizerOverlapRule>();
builder.Services.AddScoped<IBookingValidator, BookingValidator>();

builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IUsageReportService, UsageReportService>();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

// Allow requests from the React development server
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        // Invalid request bodies get the same { code, message } shape as rule violations
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "The request is not valid." : e.ErrorMessage)
                .FirstOrDefault() ?? "The request is not valid.";

            return new BadRequestObjectResult(new ApiError(ErrorCodes.ValidationError, message));
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MeetingRoomContext>();
    var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

    DbSeeder.Seed(context, clock.GetUtcNow().UtcDateTime);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactFrontend");

app.MapControllers();

app.Run();
