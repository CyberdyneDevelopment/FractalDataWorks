namespace Fdw.Protocols.Tds;

/// <summary>
/// The data type bytes of a TYPE_INFO (MS-TDS 2.2.5.4).
/// </summary>
/// <remarks>
/// Every type a client may send as a parameter is named here, so a parameter is read or refused by
/// its type; the types this server writes are the <see cref="Types.SqlDataTypes"/> options.
/// </remarks>
public static class TdsTypeIds
{
    /// <summary>A NULL with no type.</summary>
    public const byte Null = 0x1F;

    /// <summary>tinyint, fixed length.</summary>
    public const byte Int1 = 0x30;

    /// <summary>bit, fixed length.</summary>
    public const byte Bit = 0x32;

    /// <summary>smallint, fixed length.</summary>
    public const byte Int2 = 0x34;

    /// <summary>int, fixed length.</summary>
    public const byte Int4 = 0x38;

    /// <summary>smalldatetime, fixed length.</summary>
    public const byte DateTime4 = 0x3A;

    /// <summary>real, fixed length.</summary>
    public const byte Float4 = 0x3B;

    /// <summary>money, fixed length.</summary>
    public const byte Money = 0x3C;

    /// <summary>datetime, fixed length.</summary>
    public const byte DateTime = 0x3D;

    /// <summary>float, fixed length.</summary>
    public const byte Float8 = 0x3E;

    /// <summary>smallmoney, fixed length.</summary>
    public const byte Money4 = 0x7A;

    /// <summary>bigint, fixed length (MS-TDS INT8TYPE).</summary>
    public const byte BigInt = 0x7F;

    /// <summary>uniqueidentifier (MS-TDS GUIDTYPE).</summary>
    public const byte UniqueIdentifier = 0x24;

    /// <summary>A nullable integer of 1, 2, 4 or 8 bytes.</summary>
    public const byte IntN = 0x26;

    /// <summary>decimal (legacy, MS-TDS DECIMALTYPE).</summary>
    public const byte LegacyDecimal = 0x37;

    /// <summary>numeric (legacy).</summary>
    public const byte Numeric = 0x3F;

    /// <summary>A nullable bit.</summary>
    public const byte BitN = 0x68;

    /// <summary>A nullable decimal.</summary>
    public const byte DecimalN = 0x6A;

    /// <summary>A nullable numeric.</summary>
    public const byte NumericN = 0x6C;

    /// <summary>A nullable float of 4 or 8 bytes.</summary>
    public const byte FloatN = 0x6D;

    /// <summary>A nullable money of 4 or 8 bytes.</summary>
    public const byte MoneyN = 0x6E;

    /// <summary>A nullable datetime of 4 or 8 bytes.</summary>
    public const byte DateTimeN = 0x6F;

    /// <summary>date.</summary>
    public const byte DateN = 0x28;

    /// <summary>time(n).</summary>
    public const byte TimeN = 0x29;

    /// <summary>datetime2(n).</summary>
    public const byte DateTime2N = 0x2A;

    /// <summary>datetimeoffset(n).</summary>
    public const byte DateTimeOffsetN = 0x2B;

    /// <summary>char (legacy, MS-TDS CHARTYPE).</summary>
    public const byte LegacyChar = 0x2F;

    /// <summary>varchar (legacy).</summary>
    public const byte VarChar = 0x27;

    /// <summary>binary (legacy).</summary>
    public const byte Binary = 0x2D;

    /// <summary>varbinary (legacy).</summary>
    public const byte VarBinary = 0x25;

    /// <summary>varbinary(n) and varbinary(max).</summary>
    public const byte BigVarBinary = 0xA5;

    /// <summary>varchar(n) and varchar(max).</summary>
    public const byte BigVarChar = 0xA7;

    /// <summary>binary(n).</summary>
    public const byte BigBinary = 0xAD;

    /// <summary>char(n).</summary>
    public const byte BigChar = 0xAF;

    /// <summary>nvarchar(n) and nvarchar(max).</summary>
    public const byte NVarChar = 0xE7;

    /// <summary>nchar(n).</summary>
    public const byte NChar = 0xEF;

    /// <summary>text.</summary>
    public const byte Text = 0x23;

    /// <summary>image.</summary>
    public const byte Image = 0x22;

    /// <summary>ntext.</summary>
    public const byte NText = 0x63;

    /// <summary>sql_variant.</summary>
    public const byte Variant = 0x62;

    /// <summary>xml.</summary>
    public const byte Xml = 0xF1;

    /// <summary>A CLR user-defined type.</summary>
    public const byte Udt = 0xF0;

    /// <summary>The maximum-length marker of a (max) type's USHORTLEN.</summary>
    public const ushort MaxLengthMarker = 0xFFFF;
}
