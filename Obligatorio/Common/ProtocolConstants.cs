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
    public const int FixedHeaderSize = HeaderLength + CommandLength + DataLengthSize; // 9 bytes

    // Commands (2 bytes - short)
    // Rango 0-9: Autenticación y generales
    public const short CommandLogin = 1;
    public const short CommandLogout = 2;
    public const short CommandCreateUser = 3;

    // Rango 10-19: Administración de Clases
    public const short CommandCreateClass = 10;
    public const short CommandListClasses = 11;
    public const short CommandSubscribeToClass = 12;
    
    // Agrega aquí más comandos a medida que los necesites...
}