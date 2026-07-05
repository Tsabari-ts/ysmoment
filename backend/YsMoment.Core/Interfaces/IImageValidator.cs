namespace YsMoment.Core.Interfaces;

public interface IImageValidator
{
    (bool IsValid, string? Error) Validate(Stream stream, string fileName, long fileSize);
}
