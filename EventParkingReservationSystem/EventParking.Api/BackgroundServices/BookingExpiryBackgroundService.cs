using EventParking.Business.Interfaces;
using EventParking.Business.Options;

namespace EventParking.Api.BackgroundServices;

public sealed class BookingExpiryBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BookingOptions _options;
    private readonly ILogger<BookingExpiryBackgroundService> _logger;

    public BookingExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        BookingOptions options,
        ILogger<BookingExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (_options.ExpiryScanSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Booking expiry scan interval must be greater than zero.");
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.ExpiryScanSeconds));

        await ScanAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ScanAsync(stoppingToken);
        }
    }

    private async Task ScanAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var bookingService =
                scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            var expiredCount =
                await bookingService.ExpirePendingBookingsAsync(
                    cancellationToken);

            if (expiredCount > 0)
            {
                _logger.LogInformation(
                    "Expired {ExpiredBookingCount} pending bookings.",
                    expiredCount);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Booking expiry scan failed. The next scheduled scan will retry.");
        }
    }
}
