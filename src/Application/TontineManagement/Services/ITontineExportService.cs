namespace Application.TontineManagement.Services;

public interface ITontineExportService
{
    Task<byte[]> GeneratePdfAsync(Guid tontineId, CancellationToken cancellationToken = default);
}
