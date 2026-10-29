var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CarProject>("carproject");

builder.Build().Run();

