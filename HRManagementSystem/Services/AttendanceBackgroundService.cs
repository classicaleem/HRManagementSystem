namespace HRManagementSystem.Services
{
    public class AttendanceBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<AttendanceBackgroundService> _logger;

        public AttendanceBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<AttendanceBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var attendanceProcessor = scope.ServiceProvider.GetRequiredService<IAttendanceProcessorService>();

                    await attendanceProcessor.ProcessTodayAttendanceAsync();

                    // Wait for 3 minutes
                    await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in attendance background service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait 1 minute before retry
                }
            }
        }
    }
}