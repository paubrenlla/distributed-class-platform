namespace Common;

public static class ProtocolConstants
{
    // HEADER (3 bytes)
    public const string Request = "REQ";
    public const string Response = "RES";

    // Largo Fijo de Campos
    public const int HeaderLength = 3;
    public const int CommandLength = 2;
    public const int DataLengthSize = 4;
    public const int FixedHeaderSize = HeaderLength + CommandLength + DataLengthSize;

    // Rango 0-9: Autenticación y generales
    public const short CommandLogin = 1;
    public const short CommandLogout = 2;
    public const short CommandCreateUser = 3;

    // Rango 10-19: Administración de Clases
    public const short CommandCreateClass = 10;
    public const short CommandListClasses = 11;
    public const short CommandSubscribeToClass = 12;
    public const short CommandCancelSubscription = 13;
    public const short CommandShowHistory = 14;
    public const short CommandModifyClass = 15;
    public const short CommandDeleteClass = 16;
    public const short SearchAvailableClasses = 17;
    public const short SearchClassesByNamwe = 18;
    public const short SearchClassesByDescription = 19;
    public const short SearchClassesByAvailabilty = 20;
   
    public const short CommandUploadImage = 30;
    public const short CommandDownloadImage = 31;

    public const int MaxFilePartSize = 32768;
    public const int FileNameLengthSize = 4;
    public const int ClassIdSize = 4;
    public const int FileLengthSize = 8;

    public static long CalculateFileParts(long fileSize)
    {
        long fileParts = fileSize / MaxFilePartSize;
        return fileParts * MaxFilePartSize == fileSize ? fileParts : fileParts + 1;
    }

    
}