using CourseAcademy.Streaming.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<WorkerSettings>().Bind(builder.Configuration.GetSection(WorkerSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton<FfmpegTools>();
builder.Services.AddHostedService<TranscodeWorker>();

await builder.Build().RunAsync();
