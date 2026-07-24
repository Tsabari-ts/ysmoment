namespace YsMoment.Core.Interfaces;

public sealed record ImageUploadJob(Guid OrderId, Guid EventId, byte[] ImageBytes, string FileName);

public interface IImageUploadQueue
{
    void Enqueue(ImageUploadJob job);
}
