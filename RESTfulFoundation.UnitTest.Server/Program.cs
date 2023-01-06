using Microsoft.EntityFrameworkCore;
using RESTfulFoundation.UnitTest.Server;
using RESTfulFoundation.UnitTest.Server.Entities;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EFContext>
(o => o.UseInMemoryDatabase("UnitTestDatabase"));

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    using (var db = scope.ServiceProvider.GetService<EFContext>())
    {
        if (db == null) throw new Exception("EFContext Failed Init");

        var p1 = new Player
        {
            PlayerId = 1,
            PlayerName = "Mickey Mouse"
        };

        var p2 = new Player
        {
            PlayerId = 2,
            PlayerName = "Donald Duck"
        };

        var p3 = new Player
        {
            PlayerId = 3,
            PlayerName = "Minnie Mouse"
        };

        var p4 = new Player
        {
            PlayerId = 4,
            PlayerName = "Daisy Duck"
        };

        var p5 = new Player
        {
            PlayerId = 5,
            PlayerName = "Goofy"
        };

        var p6 = new Player
        {
            PlayerId = 6,
            PlayerName = "Pluto"
        };

        db.Players.Add(p1);
        db.Players.Add(p2);
        db.Players.Add(p3);
        db.Players.Add(p4);
        db.Players.Add(p5);
        db.Players.Add(p6);

        db.SaveChanges();
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
